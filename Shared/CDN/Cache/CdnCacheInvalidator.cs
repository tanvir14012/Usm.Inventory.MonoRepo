using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Usm.Shared.Caching.Abstractions;
using Usm.Shared.Infrastructure.CDN.Abstractions;

namespace Usm.Shared.Infrastructure.CDN.Cache;

/// <summary>
/// Invalidates CDN caches across all application nodes using Redis Pub/Sub.
///
/// Message protocol (published to channel "cdn:invalidation"):
///   "all"                → full cache flush
///   "pattern:{glob}"     → pattern-based flush (Redis SCAN + DEL)
///   "{assetKey}"         → single asset flush
///
/// Each node subscribes at startup via <see cref="SubscribeToInvalidationEventsAsync"/>.
/// This ensures that edge nodes running multiple replicas converge on the same cache state
/// without requiring a shared distributed lock.
/// </summary>
internal sealed class CdnCacheInvalidator(
    ICacheService cacheService,
    IConnectionMultiplexer redis,
    ILogger<CdnCacheInvalidator> logger) : ICdnCacheInvalidator
{
    private const string InvalidationChannel = "cdn:invalidation";
    private const string MetaPattern = "cdn:meta:";
    private const string VariantPattern = "cdn:variant:";

    public async ValueTask InvalidateAssetAsync(string assetKey, CancellationToken ct = default)
    {
        await cacheService.RemoveByPatternAsync($"{MetaPattern}{assetKey}*", ct).ConfigureAwait(false);
        await cacheService.RemoveByPatternAsync($"{VariantPattern}*{assetKey}*", ct).ConfigureAwait(false);

        await PublishAsync(assetKey).ConfigureAwait(false);

        logger.LogDebug("[CDN Invalidate] Asset: {AssetKey}", assetKey);
    }

    public async ValueTask InvalidatePatternAsync(string pattern, CancellationToken ct = default)
    {
        await cacheService.RemoveByPatternAsync($"{MetaPattern}{pattern}", ct).ConfigureAwait(false);
        await cacheService.RemoveByPatternAsync($"{VariantPattern}*{pattern}*", ct).ConfigureAwait(false);

        await PublishAsync($"pattern:{pattern}").ConfigureAwait(false);

        logger.LogInformation("[CDN Invalidate] Pattern: {Pattern}", pattern);
    }

    public async ValueTask InvalidateAllAsync(CancellationToken ct = default)
    {
        await cacheService.RemoveByPatternAsync("cdn:*", ct).ConfigureAwait(false);
        await PublishAsync("all").ConfigureAwait(false);

        logger.LogWarning("[CDN Invalidate] Full CDN cache flush requested");
    }

    public async ValueTask SubscribeToInvalidationEventsAsync(
        Func<string, ValueTask> handler, CancellationToken ct = default)
    {
        var subscriber = redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            RedisChannel.Literal(InvalidationChannel),
            async (_, message) =>
            {
                if (ct.IsCancellationRequested)
                    return;
                try
                {
                    await handler(message.ToString()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[CDN Invalidate] Handler error for message: {Msg}", message);
                }
            }).ConfigureAwait(false);

        logger.LogInformation(
            "[CDN Invalidate] Subscribed to Redis channel '{Channel}'", InvalidationChannel);
    }

    private async ValueTask PublishAsync(string message)
    {
        try
        {
            var sub = redis.GetSubscriber();
            await sub.PublishAsync(
                RedisChannel.Literal(InvalidationChannel),
                message,
                CommandFlags.FireAndForget).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[CDN Invalidate] Pub/Sub publish failed for message: {Msg}", message);
        }
    }
}
