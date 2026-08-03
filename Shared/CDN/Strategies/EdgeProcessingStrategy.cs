using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Infrastructure.CDN.Abstractions;
using Usm.Shared.Infrastructure.CDN.Cache;
using Usm.Shared.Infrastructure.CDN.Models;
using Usm.Shared.Infrastructure.CDN.Options;

namespace Usm.Shared.Infrastructure.CDN.Strategies;

/// <summary>
/// Performs real-time image transformations at the CDN edge (resize, format conversion).
///
/// Flow:
///   1. Compute the variant cache key from the MediaProcessingRequest.
///   2. Check Redis for a previously processed variant (metadata + binary payload).
///   3. On HIT  → return the cached InlineData to the HTTP response layer.
///   4. On MISS → fetch the original from storage, apply the IMediaProcessor pipeline,
///               store both the MediaVariant metadata and the binary payload in Redis,
///               then return the InlineData.
///
/// Priority: 1 (highest – evaluated before any routing strategy).
/// Activates when: EdgeProcessing is enabled AND the request carries image transformation parameters.
/// </summary>
internal sealed class EdgeProcessingStrategy(
    IStorageProviderEngine storageEngine,
    IMediaProcessor imageProcessor,
    AssetCacheManager cacheManager,
    IOptions<CdnOptions> options,
    ILogger<EdgeProcessingStrategy> logger) : ICdnDistributionStrategy
{
    private readonly CdnOptions _opts = options.Value;

    public string Name => "EdgeProcessing";
    public int Priority => 1;

    public bool CanHandle(DistributionContext context)
        => _opts.EnableEdgeProcessing && context.ProcessingRequest?.CacheKey is not null;

    public async ValueTask<DistributionResult> ExecuteAsync(
        DistributionContext context, CancellationToken cancellationToken = default)
    {
        var request = context.ProcessingRequest!;
        var variantSuffix = request.CacheKey!;
        var variantKey = $"{context.Bucket}:{context.AssetKey}:{variantSuffix}";

        // ── 1. Check variant cache ────────────────────────────────────────────
        var cachedVariant = await cacheManager
            .GetVariantAsync(context.Bucket, context.AssetKey, variantSuffix, cancellationToken)
            .ConfigureAwait(false);

        if (cachedVariant is not null)
        {
            var cachedData = await cacheManager
                .GetVariantDataAsync(variantKey, cancellationToken)
                .ConfigureAwait(false);

            if (cachedData is not null)
            {
                logger.LogDebug("[{Strategy}] Variant cache HIT: {VKey}", Name, variantKey);
                return BuildResult(context, cachedVariant, cachedData, cachedVariant.ContentType, servedFromCache: true);
            }
        }

        // ── 2. Cache miss – fetch original from storage ───────────────────────
        var metadata = await storageEngine
            .GetMetadataAsync(context.Bucket, context.AssetKey, cancellationToken)
            .ConfigureAwait(false);

        if (metadata is null || !imageProcessor.CanProcess(metadata.ContentType, request))
        {
            logger.LogDebug("[{Strategy}] Cannot process '{CT}' – falling through", Name, metadata?.ContentType);
            return BuildResult(context, processedVariant: null, inlineData: null,
                inlineContentType: null, servedFromCache: false);
        }

        await using var sourceStream = await storageEngine
            .GetObjectStreamAsync(context.Bucket, context.AssetKey, cancellationToken)
            .ConfigureAwait(false);

        if (sourceStream is null)
            return BuildResult(context, null, null, null, servedFromCache: false);

        // ── 3. Transform ──────────────────────────────────────────────────────
        var processed = await imageProcessor
            .ProcessAsync(sourceStream, metadata.ContentType, request, cancellationToken)
            .ConfigureAwait(false);

        // ── 4. Persist variant in Redis ───────────────────────────────────────
        var variant = new MediaVariant
        {
            OriginalKey = context.AssetKey,
            VariantKey = variantKey,
            ContentType = processed.ContentType,
            Width = processed.Width,
            Height = processed.Height,
            Size = processed.Data.Length,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await cacheManager
            .SetVariantAsync(context.Bucket, context.AssetKey, variantSuffix, variant, cancellationToken)
            .ConfigureAwait(false);
        await cacheManager
            .SetVariantDataAsync(variantKey, processed.Data, cancellationToken)
            .ConfigureAwait(false);

        logger.LogDebug("[{Strategy}] Generated variant {VKey}: {W}x{H} {Size} bytes",
            Name, variantKey, processed.Width, processed.Height, processed.Data.Length);

        return BuildResult(context, variant, processed.Data, processed.ContentType, servedFromCache: false);
    }

    private DistributionResult BuildResult(
        DistributionContext context,
        MediaVariant? processedVariant,
        byte[]? inlineData,
        string? inlineContentType,
        bool servedFromCache)
    {
        var providerOpts = _opts.StorageProviders.FirstOrDefault()
            ?? throw new InvalidOperationException("No storage providers configured.");

        return new DistributionResult
        {
            StrategyUsed = Name,
            Endpoint = new StorageEndpoint
            {
                ProviderName = providerOpts.Name,
                BaseUrl = providerOpts.Endpoint,
                Bucket = providerOpts.DefaultBucket,
                Region = providerOpts.Region,
                Priority = providerOpts.Priority
            },
            ProcessedVariant = processedVariant,
            InlineData = inlineData,
            InlineContentType = inlineContentType,
            ServedFromCache = servedFromCache
        };
    }
}
