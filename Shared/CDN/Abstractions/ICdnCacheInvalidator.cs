namespace Usm.Shared.Infrastructure.CDN.Abstractions;

/// <summary>
/// Invalidates CDN asset caches (metadata, image variants, signed URL cache) across all
/// application nodes using Redis Pub/Sub.
/// </summary>
public interface ICdnCacheInvalidator
{
    /// <summary>Invalidates all cached data for a specific asset key.</summary>
    ValueTask InvalidateAssetAsync(string assetKey, CancellationToken ct = default);

    /// <summary>Invalidates all cached data for asset keys matching a Redis glob pattern.</summary>
    ValueTask InvalidatePatternAsync(string pattern, CancellationToken ct = default);

    /// <summary>Performs a full CDN cache flush (use with caution in production).</summary>
    ValueTask InvalidateAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Subscribes to the Redis invalidation channel and invokes <paramref name="handler"/>
    /// for each incoming invalidation message.  Call once at startup.
    /// </summary>
    ValueTask SubscribeToInvalidationEventsAsync(
        Func<string, ValueTask> handler, CancellationToken ct = default);
}
