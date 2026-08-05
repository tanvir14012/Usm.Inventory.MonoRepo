using Usm.Shared.Patterns.Cache;

namespace Usm.Shared.Patterns.Cache.Abstractions;

/// <summary>
/// Fluent builder for configuring a cache instance.
/// </summary>
/// <typeparam name="TKey">The cache key type.</typeparam>
/// <typeparam name="TValue">The cache value type.</typeparam>
public interface ICacheBuilder<TKey, TValue>
    where TKey : notnull
{
    /// <summary>Sets the eviction policy to LRU.</summary>
    ICacheBuilder<TKey, TValue> UseLru();

    /// <summary>Sets the eviction policy to LFU.</summary>
    ICacheBuilder<TKey, TValue> UseLfu();

    /// <summary>Sets the maximum number of entries.</summary>
    ICacheBuilder<TKey, TValue> WithCapacity(int capacity);

    /// <summary>Sets the default time-to-live for new entries.</summary>
    ICacheBuilder<TKey, TValue> WithDefaultExpiration(TimeSpan? expiration);

    /// <summary>Sets the time provider used for expiration.</summary>
    ICacheBuilder<TKey, TValue> WithTimeProvider(TimeProvider timeProvider);

    /// <summary>Sets the metrics collector.</summary>
    ICacheBuilder<TKey, TValue> WithMetrics(CacheMetrics metrics);

    /// <summary>Builds the configured cache.</summary>
    ICache<TKey, TValue> Build();
}
