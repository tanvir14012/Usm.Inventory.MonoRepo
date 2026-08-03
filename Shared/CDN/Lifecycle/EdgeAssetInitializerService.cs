using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Infrastructure.CDN.Abstractions;
using Usm.Shared.Infrastructure.CDN.Models;
using Usm.Shared.Infrastructure.CDN.Options;

namespace Usm.Shared.Infrastructure.CDN.Lifecycle;

/// <summary>
/// <see cref="IHostedService"/> that runs once at application startup to:
///   1. Ensure all configured storage buckets exist across all providers.
///   2. Scan the <c>ManifestDirectory</c> for <c>*.json</c> manifest files.
///   3. For each manifest: configure CORS + lifecycle policies, then idempotently
///      upload any listed static assets that do not yet exist in the target bucket.
///
/// Operations are idempotent: re-deploying will not overwrite existing assets or
/// re-apply policies that are already in place (existence is checked before each write).
/// </summary>
internal sealed class EdgeAssetInitializerService(
    IEnumerable<IStorageProvider> storageProviders,
    IOptions<CdnOptions> options,
    ILogger<EdgeAssetInitializerService> logger)
    : IHostedService, IEdgeAssetInitializer
{
    private readonly IReadOnlyList<IStorageProvider> _providers = [.. storageProviders];
    private readonly CdnOptions _opts = options.Value;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    // ── IHostedService ───────────────────────────────────────────────────────

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("[CDN Init] Initialization cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CDN Init] Edge asset initialization failed – service will continue");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ── IEdgeAssetInitializer ────────────────────────────────────────────────

    public async ValueTask InitializeAsync(CancellationToken ct = default)
    {
        logger.LogInformation("[CDN Init] Starting edge asset initialization…");

        // Step 1: Ensure all configured default buckets exist
        await EnsureDefaultBucketsAsync(ct).ConfigureAwait(false);

        // Step 2: Process JSON manifests
        if (!Directory.Exists(_opts.ManifestDirectory))
        {
            logger.LogDebug("[CDN Init] Manifest directory '{Dir}' not found – skipping manifest phase",
                _opts.ManifestDirectory);
        }
        else
        {
            var manifests = Directory.GetFiles(_opts.ManifestDirectory, "*.json",
                SearchOption.TopDirectoryOnly);

            logger.LogInformation("[CDN Init] Found {Count} manifest(s) to process", manifests.Length);

            foreach (var path in manifests)
            {
                ct.ThrowIfCancellationRequested();
                await ProcessManifestFileAsync(path, ct).ConfigureAwait(false);
            }
        }

        logger.LogInformation("[CDN Init] Edge asset initialization complete");
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async ValueTask EnsureDefaultBucketsAsync(CancellationToken ct)
    {
        var tasks = _opts.StorageProviders
            .Where(p => !string.IsNullOrEmpty(p.DefaultBucket))
            .SelectMany(provOpts => _providers
                .Where(p => p.Name == provOpts.Name)
                .Select(p => SafeEnsureBucketAsync(p, provOpts.DefaultBucket, ct)));

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task SafeEnsureBucketAsync(IStorageProvider provider, string bucket, CancellationToken ct)
    {
        try
        {
            await provider.EnsureBucketAsync(bucket, ct).ConfigureAwait(false);
            logger.LogDebug("[CDN Init] Bucket '{Bucket}' ensured on '{Provider}'", bucket, provider.Name);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[CDN Init] Could not ensure bucket '{Bucket}' on '{Provider}'",
                bucket, provider.Name);
        }
    }

    private async ValueTask ProcessManifestFileAsync(string filePath, CancellationToken ct)
    {
        EdgeManifest manifest;
        try
        {
            await using var stream = File.OpenRead(filePath);
            manifest = await JsonSerializer.DeserializeAsync<EdgeManifest>(stream, JsonOpts, ct)
                .ConfigureAwait(false)
                ?? throw new InvalidDataException("Manifest file deserialized to null.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CDN Init] Failed to parse manifest '{File}'", filePath);
            return;
        }

        logger.LogInformation("[CDN Init] Processing manifest '{Id}' ({Name}) → bucket '{Bucket}'",
            manifest.ManifestId, manifest.Name, manifest.Bucket);

        // Use first writable provider, or any provider as fallback
        var provider = _providers.FirstOrDefault(p => !p.IsReadOnly) ?? _providers.FirstOrDefault();
        if (provider is null)
        {
            logger.LogWarning("[CDN Init] No provider available for manifest '{Id}'", manifest.ManifestId);
            return;
        }

        await SafeEnsureBucketAsync(provider, manifest.Bucket, ct).ConfigureAwait(false);

        if (manifest.Cors is not null)
        {
            try
            {
                await provider.ConfigureBucketCorsAsync(manifest.Bucket, manifest.Cors, ct).ConfigureAwait(false);
                logger.LogDebug("[CDN Init] CORS applied to bucket '{Bucket}'", manifest.Bucket);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[CDN Init] CORS configuration failed for bucket '{Bucket}'", manifest.Bucket);
            }
        }

        if (manifest.Lifecycle is not null)
        {
            try
            {
                await provider.ConfigureBucketLifecycleAsync(manifest.Bucket, manifest.Lifecycle, ct).ConfigureAwait(false);
                logger.LogDebug("[CDN Init] Lifecycle rules applied to bucket '{Bucket}'", manifest.Bucket);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[CDN Init] Lifecycle configuration failed for bucket '{Bucket}'", manifest.Bucket);
            }
        }

        foreach (var entry in manifest.Entries)
        {
            ct.ThrowIfCancellationRequested();
            await ProcessManifestEntryAsync(provider, manifest.Bucket, entry, ct).ConfigureAwait(false);
        }
    }

    private async ValueTask ProcessManifestEntryAsync(
        IStorageProvider provider, string bucket, EdgeManifestEntry entry, CancellationToken ct)
    {
        if (!File.Exists(entry.SourcePath))
        {
            logger.LogDebug("[CDN Init] Source '{Path}' for entry '{Key}' not found – skipping",
                entry.SourcePath, entry.Key);
            return;
        }

        // Idempotency: skip objects that already exist
        bool exists;
        try { exists = await provider.ObjectExistsAsync(bucket, entry.Key, ct).ConfigureAwait(false); }
        catch { exists = false; }

        if (exists)
        {
            logger.LogDebug("[CDN Init] Entry '{Key}' already exists in '{Bucket}' – skipping", entry.Key, bucket);
            return;
        }

        var contentType = entry.ContentType ?? DetectContentType(entry.SourcePath);
        await using var fileStream = new FileStream(
            entry.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65_536, true);

        try
        {
            await provider.PutObjectAsync(bucket, entry.Key, fileStream, contentType, entry.Metadata, ct)
                .ConfigureAwait(false);
            logger.LogInformation("[CDN Init] Uploaded '{Key}' → '{Bucket}'", entry.Key, bucket);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CDN Init] Failed to upload entry '{Key}' to '{Bucket}'", entry.Key, bucket);
        }
    }

    private static string DetectContentType(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"  => "image/png",
            ".gif"  => "image/gif",
            ".webp" => "image/webp",
            ".avif" => "image/avif",
            ".svg"  => "image/svg+xml",
            ".mp4"  => "video/mp4",
            ".webm" => "video/webm",
            ".m3u8" => "application/x-mpegURL",
            ".ts"   => "video/mp2t",
            ".css"  => "text/css",
            ".js"   => "application/javascript",
            ".html" => "text/html",
            ".json" => "application/json",
            ".pdf"  => "application/pdf",
            ".woff2"=> "font/woff2",
            ".woff" => "font/woff",
            _       => "application/octet-stream"
        };
}
