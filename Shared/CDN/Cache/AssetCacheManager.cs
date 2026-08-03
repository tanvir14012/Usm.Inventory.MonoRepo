using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Caching.Abstractions;
using Usm.Shared.Caching.Models;
using Usm.Shared.Infrastructure.CDN.Models;
using Usm.Shared.Infrastructure.CDN.Options;

namespace Usm.Shared.Infrastructure.CDN.Cache;

/// <summary>
/// Redis-backed cache manager for CDN asset metadata, processed image variant payloads,
/// and pre-signed URL tokens.  All keys are namespaced under "cdn:" to avoid collisions
/// with other services using the same Redis instance.
/// </summary>
public sealed class AssetCacheManager(
    ICacheService cache,
    IOptions<CdnOptions> options,
    ILogger<AssetCacheManager> logger)
{
    private readonly MediaProcessingOptions _mediaOpts = options.Value.MediaProcessing;

    private const string MetaPrefix = "cdn:meta:";
    private const string VariantMetaPrefix = "cdn:variant:meta:";
    private const string VariantDataPrefix = "cdn:variant:data:";
    private const string SignedUrlPrefix = "cdn:surl:";

    private static readonly CacheEntryOptions MetaTtl =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };

    // ── Asset metadata ───────────────────────────────────────────────────────

    public async ValueTask<AssetMetadata?> GetMetadataAsync(
        string bucket, string key, CancellationToken ct = default)
    {
        var meta = await cache.GetAsync<AssetMetadata>(MetaKey(bucket, key), ct).ConfigureAwait(false);
        if (meta is not null)
            logger.LogDebug("[Cache HIT] Asset metadata {Bucket}/{Key}", bucket, key);
        return meta;
    }

    public async ValueTask SetMetadataAsync(AssetMetadata metadata, CancellationToken ct = default)
    {
        await cache.SetAsync(MetaKey(metadata.Bucket, metadata.Key), metadata, MetaTtl, ct)
            .ConfigureAwait(false);
        logger.LogDebug("[Cache SET] Asset metadata {Bucket}/{Key}", metadata.Bucket, metadata.Key);
    }

    public async ValueTask InvalidateMetadataAsync(string bucket, string key, CancellationToken ct = default)
    {
        await cache.RemoveAsync(MetaKey(bucket, key), ct).ConfigureAwait(false);
        // Also sweep all variants derived from this source asset
        await cache.RemoveByPatternAsync($"{VariantMetaPrefix}{bucket}:{key}:*", ct).ConfigureAwait(false);
        await cache.RemoveByPatternAsync($"{VariantDataPrefix}{bucket}:{key}:*", ct).ConfigureAwait(false);
    }

    // ── Image variant metadata ───────────────────────────────────────────────

    public async ValueTask<MediaVariant?> GetVariantAsync(
        string bucket, string key, string variantSuffix, CancellationToken ct = default)
        => await cache.GetAsync<MediaVariant>(VariantMetaKey(bucket, key, variantSuffix), ct)
            .ConfigureAwait(false);

    public async ValueTask SetVariantAsync(
        string bucket, string key, string variantSuffix,
        MediaVariant variant, CancellationToken ct = default)
        => await cache.SetAsync(
            VariantMetaKey(bucket, key, variantSuffix),
            variant,
            new CacheEntryOptions { AbsoluteExpirationRelativeToNow = _mediaOpts.VariantCacheTtl },
            ct).ConfigureAwait(false);

    // ── Image variant binary data ────────────────────────────────────────────

    public async ValueTask<byte[]?> GetVariantDataAsync(string variantKey, CancellationToken ct = default)
        => await cache.GetAsync<byte[]>($"{VariantDataPrefix}{variantKey}", ct).ConfigureAwait(false);

    public async ValueTask SetVariantDataAsync(
        string variantKey, byte[] data, CancellationToken ct = default)
        => await cache.SetAsync(
            $"{VariantDataPrefix}{variantKey}",
            data,
            new CacheEntryOptions { AbsoluteExpirationRelativeToNow = _mediaOpts.VariantCacheTtl },
            ct).ConfigureAwait(false);

    // ── Pre-signed URL cache ─────────────────────────────────────────────────

    public async ValueTask<string?> GetSignedUrlAsync(string assetKey, CancellationToken ct = default)
        => await cache.GetAsync<string>($"{SignedUrlPrefix}{assetKey}", ct).ConfigureAwait(false);

    public async ValueTask SetSignedUrlAsync(
        string assetKey, string signedUrl, TimeSpan ttl, CancellationToken ct = default)
        => await cache.SetAsync(
            $"{SignedUrlPrefix}{assetKey}",
            signedUrl,
            new CacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            ct).ConfigureAwait(false);

    // ── Key builders ─────────────────────────────────────────────────────────

    private static string MetaKey(string bucket, string key) => $"{MetaPrefix}{bucket}:{key}";

    private static string VariantMetaKey(string bucket, string key, string suffix)
        => $"{VariantMetaPrefix}{bucket}:{key}:{suffix}";
}
