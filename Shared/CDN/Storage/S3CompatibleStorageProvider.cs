using System.Net;
using System.Runtime.CompilerServices;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Usm.Shared.Infrastructure.CDN.Abstractions;
using Usm.Shared.Infrastructure.CDN.Models;
using Usm.Shared.Infrastructure.CDN.Options;

// Alias both types to resolve the LifecycleConfiguration name collision
using S3LifecycleConfig = Amazon.S3.Model.LifecycleConfiguration;
using S3CorsConfig = Amazon.S3.Model.CORSConfiguration;
using CdnLifecycleConfig = Usm.Shared.Infrastructure.CDN.Models.LifecycleConfiguration;
using CdnCorsConfig = Usm.Shared.Infrastructure.CDN.Models.CorsConfiguration;

namespace Usm.Shared.Infrastructure.CDN.Storage;

/// <summary>
/// IStorageProvider implementation for any S3-compatible backend:
/// AWS S3, Cloudflare R2, and MinIO (set ForcePathStyle=true + custom Endpoint).
/// </summary>
internal sealed class S3CompatibleStorageProvider : IStorageProvider, IAsyncDisposable
{
    private readonly AmazonS3Client _client;
    private readonly StorageProviderOptions _options;
    private readonly ILogger<S3CompatibleStorageProvider> _logger;

    public string Name => _options.Name;
    public int Priority => _options.Priority;
    public string[]? GeoRegions => _options.GeoRegions;
    public bool IsReadOnly => _options.IsReadOnly;

    public S3CompatibleStorageProvider(
        StorageProviderOptions options,
        ILogger<S3CompatibleStorageProvider> logger)
    {
        _options = options;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ForcePathStyle = options.ForcePathStyle,
            UseHttp = !options.UseHttps
        };

        if (!string.IsNullOrEmpty(options.Endpoint))
            config.ServiceURL = options.UseHttps
                ? options.Endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    ? options.Endpoint
                    : $"https://{options.Endpoint}"
                : options.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    ? options.Endpoint
                    : $"http://{options.Endpoint}";
        else
            config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region);

        _client = new AmazonS3Client(options.AccessKey, options.SecretKey, config);
    }

    // ── Read operations ──────────────────────────────────────────────────────

    public async ValueTask<Stream?> GetObjectStreamAsync(string bucket, string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(bucket, key, ct).ConfigureAwait(false);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Provider}] GET {Bucket}/{Key} failed", Name, bucket, key);
            throw;
        }
    }

    public async ValueTask<AssetMetadata?> GetMetadataAsync(string bucket, string key, CancellationToken ct = default)
    {
        try
        {
            var req = new GetObjectMetadataRequest { BucketName = bucket, Key = key };
            var r = await _client.GetObjectMetadataAsync(req, ct).ConfigureAwait(false);

            return new AssetMetadata
            {
                Key = key,
                Bucket = bucket,
                ContentType = r.Headers.ContentType ?? "application/octet-stream",
                Size = r.ContentLength,
                LastModified = new DateTimeOffset(r.LastModified, TimeSpan.Zero),
                ETag = r.ETag?.Trim('"'),
                StorageProvider = Name,
                Region = _options.Region,
                Metadata = r.Metadata.Keys
                    .ToDictionary(k => k, k => r.Metadata[k])
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Provider}] HEAD {Bucket}/{Key} failed", Name, bucket, key);
            throw;
        }
    }

    // ── Write operations ─────────────────────────────────────────────────────

    public async ValueTask PutObjectAsync(
        string bucket, string key, Stream content, string contentType,
        IDictionary<string, string>? metadata = null, CancellationToken ct = default)
    {
        var req = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };

        if (metadata is not null)
            foreach (var (k, v) in metadata)
                req.Metadata[k] = v;

        await _client.PutObjectAsync(req, ct).ConfigureAwait(false);
    }

    public async ValueTask DeleteObjectAsync(string bucket, string key, CancellationToken ct = default)
        => await _client.DeleteObjectAsync(bucket, key, ct).ConfigureAwait(false);

    public async ValueTask<bool> ObjectExistsAsync(string bucket, string key, CancellationToken ct = default)
    {
        try
        {
            var req = new GetObjectMetadataRequest { BucketName = bucket, Key = key };
            await _client.GetObjectMetadataAsync(req, ct).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    // ── Bucket management ────────────────────────────────────────────────────

    public async ValueTask EnsureBucketAsync(string bucket, CancellationToken ct = default)
    {
        try
        {
            await _client.GetBucketLocationAsync(bucket, ct).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (
            ex.ErrorCode is "NoSuchBucket" ||
            ex.StatusCode == HttpStatusCode.NotFound)
        {
            await _client.PutBucketAsync(
                new PutBucketRequest { BucketName = bucket, UseClientRegion = true }, ct)
                .ConfigureAwait(false);
            _logger.LogInformation("[{Provider}] Created bucket '{Bucket}'", Name, bucket);
        }
    }

    public async IAsyncEnumerable<string> ListObjectsAsync(
        string bucket, string prefix, [EnumeratorCancellation] CancellationToken ct = default)
    {
        string? token = null;
        do
        {
            ct.ThrowIfCancellationRequested();
            var req = new ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = prefix,
                ContinuationToken = token
            };
            var resp = await _client.ListObjectsV2Async(req, ct).ConfigureAwait(false);

            foreach (var obj in resp.S3Objects)
                yield return obj.Key;

            token = resp.IsTruncated ? resp.NextContinuationToken : null;
        }
        while (token is not null);
    }

    // ── Signed URLs ──────────────────────────────────────────────────────────

    public ValueTask<string?> GetPreSignedUrlAsync(
        string bucket, string key, TimeSpan expiry, CancellationToken ct = default)
    {
        var req = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiry),
            Protocol = _options.UseHttps ? Protocol.HTTPS : Protocol.HTTP,
            Verb = HttpVerb.GET
        };
        return ValueTask.FromResult<string?>(_client.GetPreSignedURL(req));
    }

    // ── Bucket policies ──────────────────────────────────────────────────────

    public async ValueTask ConfigureBucketCorsAsync(
        string bucket, CdnCorsConfig cors, CancellationToken ct = default)
    {
        var rule = new CORSRule
        {
            AllowedOrigins = [.. cors.AllowedOrigins],
            AllowedMethods = [.. cors.AllowedMethods],
            AllowedHeaders = [.. cors.AllowedHeaders],
            MaxAgeSeconds = cors.MaxAgeSeconds
        };
        await _client.PutCORSConfigurationAsync(
            new PutCORSConfigurationRequest
            {
                BucketName = bucket,
                Configuration = new S3CorsConfig { Rules = [rule] }
            }, ct).ConfigureAwait(false);
    }

    public async ValueTask ConfigureBucketLifecycleAsync(
        string bucket, CdnLifecycleConfig lifecycle, CancellationToken ct = default)
    {
        var rules = new List<LifecycleRule>();

        if (lifecycle.EnableExpiration)
            rules.Add(new LifecycleRule
            {
                Id = "cdn-auto-expire",
                Status = LifecycleRuleStatus.Enabled,
                Filter = new LifecycleFilter
                {
                    LifecycleFilterPredicate = new LifecyclePrefixPredicate { Prefix = string.Empty }
                },
                Expiration = new LifecycleRuleExpiration { Days = lifecycle.ExpirationDays }
            });

        if (lifecycle.EnableTransitionToIA)
            rules.Add(new LifecycleRule
            {
                Id = "cdn-ia-transition",
                Status = LifecycleRuleStatus.Enabled,
                Filter = new LifecycleFilter
                {
                    LifecycleFilterPredicate = new LifecyclePrefixPredicate { Prefix = string.Empty }
                },
                Transitions =
                [
                    new LifecycleTransition
                    {
                        Days = lifecycle.TransitionToIADays,
                        StorageClass = S3StorageClass.StandardInfrequentAccess
                    }
                ]
            });

        if (rules.Count == 0) return;

        await _client.PutLifecycleConfigurationAsync(
            new PutLifecycleConfigurationRequest
            {
                BucketName = bucket,
                Configuration = new S3LifecycleConfig { Rules = rules }
            }, ct).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
