using Usm.Shared.Patterns.Cache.Configuration;

namespace Usm.Shared.Patterns.Cache.Abstractions;

/// <summary>
/// Describes a thread-safe in-memory cache with async-first accessors.
/// </summary>
/// <typeparam name="TKey">The cache key type.</typeparam>
/// <typeparam name="TValue">The cache value type.</typeparam>
public interface ICache<TKey, TValue>
    where TKey : notnull
{
    /// <summary>Gets the number of active entries.</summary>
    int Count { get; }

    /// <summary>Gets the current cache metrics snapshot.</summary>
    CacheMetricsSnapshot Metrics { get; }

    /// <summary>Gets a value from the cache if present.</summary>
    bool TryGetValue(TKey key, out TValue? value);

    /// <summary>Gets a value from the cache asynchronously if present.</summary>
    ValueTask<(bool Found, TValue? Value)> TryGetValueAsync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>Stores a value in the cache.</summary>
    ValueTask SetAsync(TKey key, TValue value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Gets a value or creates it atomically when missing.</summary>
    ValueTask<TValue> GetOrCreateAsync(
        TKey key,
        Func<CancellationToken, ValueTask<TValue>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a value from the cache.</summary>
    ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>Clears all entries.</summary>
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
