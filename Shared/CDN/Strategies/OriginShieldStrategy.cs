using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Infrastructure.CDN.Abstractions;
using Usm.Shared.Infrastructure.CDN.Cache;
using Usm.Shared.Infrastructure.CDN.Models;
using Usm.Shared.Infrastructure.CDN.Options;

namespace Usm.Shared.Infrastructure.CDN.Strategies;

/// <summary>
/// Implements the Origin-Shield (tiered-cache) pattern to prevent thundering-herd
/// cache stampedes.
///
/// On cache HIT  → returns metadata from Redis without touching storage.
/// On cache MISS → fetches metadata from the origin storage, populates the Redis shield
///                 cache, and returns.  Concurrent misses for the same key are serialised
///                 by the double-checked locking inside ICacheService.GetOrCreateAsync.
///
/// Priority: 5 (evaluated before regional/load strategies so the shield intercepts first).
/// Activates when: always applicable (every request can benefit from origin shielding).
/// </summary>
internal sealed class OriginShieldStrategy(
    IStorageProviderEngine storageEngine,
    AssetCacheManager cacheManager,
    IOptions<CdnOptions> options,
    ILogger<OriginShieldStrategy> logger) : ICdnDistributionStrategy
{
    private readonly CdnOptions _opts = options.Value;

    public string Name => "OriginShield";
    public int Priority => 5;
    public bool CanHandle(DistributionContext context) => true;

    public async ValueTask<DistributionResult> ExecuteAsync(
        DistributionContext context, CancellationToken cancellationToken = default)
    {
        // Prefer metadata already supplied on the context (e.g. from a pre-flight cache check)
        var metadata = context.CachedMetadata
            ?? await cacheManager
                .GetMetadataAsync(context.Bucket, context.AssetKey, cancellationToken)
                .ConfigureAwait(false);

        var cacheHit = metadata is not null;

        if (!cacheHit)
        {
            logger.LogDebug("[{Strategy}] Cache MISS for {Key} – fetching from origin", Name, context.AssetKey);
            metadata = await storageEngine
                .GetMetadataAsync(context.Bucket, context.AssetKey, cancellationToken)
                .ConfigureAwait(false);

            if (metadata is not null)
                await cacheManager.SetMetadataAsync(metadata, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            logger.LogDebug("[{Strategy}] Cache HIT for {Key}", Name, context.AssetKey);
        }

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
            ResolvedMetadata = metadata,
            ServedFromCache = cacheHit
        };
    }
}
