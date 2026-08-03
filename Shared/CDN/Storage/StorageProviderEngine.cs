using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Infrastructure.CDN.Abstractions;
using Usm.Shared.Infrastructure.CDN.Models;
using Usm.Shared.Infrastructure.CDN.Options;

namespace Usm.Shared.Infrastructure.CDN.Storage;

/// <summary>
/// High-level storage facade that routes operations across multiple <see cref="IStorageProvider"/>
/// instances with per-provider circuit-breaking and automatic failover.
///
/// Read operations: iterate providers by priority until one succeeds.
/// Write operations: target the highest-priority writable provider only (mirrors are written
///   asynchronously via a replication layer if added later).
/// Delete operations: fan-out to all writable providers.
/// </summary>
internal sealed class StorageProviderEngine : IStorageProviderEngine
{
    private readonly IReadOnlyList<IStorageProvider> _providers;
    private readonly Dictionary<string, CircuitBreakerState> _breakers;
    private readonly Lock _breakerLock = new();
    private readonly ILogger<StorageProviderEngine> _logger;

    public StorageProviderEngine(
        IEnumerable<IStorageProvider> providers,
        IOptions<CdnOptions> options,
        ILogger<StorageProviderEngine> logger)
    {
        _logger = logger;
        _providers = [.. providers.OrderBy(p => p.Priority)];

        var opts = options.Value;
        _breakers = _providers.ToDictionary(
            p => p.Name,
            p => new CircuitBreakerState(
                opts.CircuitBreakerFailureThreshold,
                opts.CircuitBreakerOpenDuration),
            StringComparer.Ordinal);
    }

    // ── Read operations ──────────────────────────────────────────────────────

    public async ValueTask<Stream?> GetObjectStreamAsync(
        string bucket, string key, CancellationToken ct = default)
    {
        foreach (var p in HealthyProviders())
        {
            var cb = Breaker(p.Name);
            try
            {
                var stream = await p.GetObjectStreamAsync(bucket, key, ct).ConfigureAwait(false);
                cb.OnSuccess();
                return stream;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                cb.OnFailure();
                _logger.LogWarning(ex, "[{Provider}] GET {Bucket}/{Key} failed, trying failover",
                    p.Name, bucket, key);
            }
        }
        return null;
    }

    public async ValueTask<AssetMetadata?> GetMetadataAsync(
        string bucket, string key, CancellationToken ct = default)
    {
        foreach (var p in HealthyProviders())
        {
            var cb = Breaker(p.Name);
            try
            {
                var meta = await p.GetMetadataAsync(bucket, key, ct).ConfigureAwait(false);
                cb.OnSuccess();
                return meta;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                cb.OnFailure();
                _logger.LogWarning(ex, "[{Provider}] HEAD {Bucket}/{Key} failed, trying failover",
                    p.Name, bucket, key);
            }
        }
        return null;
    }

    // ── Write operations ─────────────────────────────────────────────────────

    public async ValueTask PutObjectAsync(
        string bucket, string key, Stream content, string contentType,
        IDictionary<string, string>? metadata = null, CancellationToken ct = default)
    {
        var writable = HealthyProviders().Where(p => !p.IsReadOnly).ToList();
        if (writable.Count == 0)
            throw new InvalidOperationException("No healthy writable storage provider is available.");

        var primary = writable[0];
        var cb = Breaker(primary.Name);
        try
        {
            await primary.PutObjectAsync(bucket, key, content, contentType, metadata, ct)
                .ConfigureAwait(false);
            cb.OnSuccess();
        }
        catch (Exception ex)
        {
            cb.OnFailure();
            throw new IOException(
                $"Primary storage provider '{primary.Name}' failed on PUT '{bucket}/{key}'.", ex);
        }
    }

    public async ValueTask DeleteObjectAsync(string bucket, string key, CancellationToken ct = default)
    {
        var tasks = HealthyProviders()
            .Where(p => !p.IsReadOnly)
            .Select(async p =>
            {
                try
                {
                    await p.DeleteObjectAsync(bucket, key, ct).ConfigureAwait(false);
                    Breaker(p.Name).OnSuccess();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Breaker(p.Name).OnFailure();
                    _logger.LogWarning(ex, "[{Provider}] DELETE {Bucket}/{Key} failed",
                        p.Name, bucket, key);
                }
            });
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async ValueTask<bool> ObjectExistsAsync(
        string bucket, string key, CancellationToken ct = default)
    {
        foreach (var p in HealthyProviders())
        {
            var cb = Breaker(p.Name);
            try
            {
                var exists = await p.ObjectExistsAsync(bucket, key, ct).ConfigureAwait(false);
                cb.OnSuccess();
                return exists;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                cb.OnFailure();
                _logger.LogWarning(ex, "[{Provider}] EXISTS {Bucket}/{Key} failed",
                    p.Name, bucket, key);
            }
        }
        return false;
    }

    public async ValueTask EnsureBucketAsync(string bucket, CancellationToken ct = default)
    {
        var tasks = _providers.Select(async p =>
        {
            try
            {
                await p.EnsureBucketAsync(bucket, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "[{Provider}] EnsureBucket '{Bucket}' failed", p.Name, bucket);
            }
        });
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<string> ListObjectsAsync(
        string bucket, string prefix, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var p in HealthyProviders())
        {
            var listed = false;
            await foreach (var key in p.ListObjectsAsync(bucket, prefix, ct))
            {
                listed = true;
                yield return key;
            }
            if (listed)
                yield break; // Only list from the primary healthy provider
        }
    }

    public async ValueTask<string?> GetPreSignedUrlAsync(
        string bucket, string key, TimeSpan expiry, CancellationToken ct = default)
    {
        foreach (var p in HealthyProviders())
        {
            var cb = Breaker(p.Name);
            try
            {
                var url = await p.GetPreSignedUrlAsync(bucket, key, expiry, ct).ConfigureAwait(false);
                cb.OnSuccess();
                if (url is not null)
                    return url;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                cb.OnFailure();
            }
        }
        return null;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IEnumerable<IStorageProvider> HealthyProviders()
        => _providers.Where(p => Breaker(p.Name).IsAllowed());

    private CircuitBreakerState Breaker(string name)
    {
        lock (_breakerLock)
            return _breakers.TryGetValue(name, out var cb) ? cb : throw new KeyNotFoundException(name);
    }
}
