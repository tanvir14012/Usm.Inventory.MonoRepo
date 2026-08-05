using Usm.Shared.Patterns.Cache;
using Usm.Shared.Patterns.Cache.Abstractions;
using Usm.Shared.Patterns.Cache.Configuration;

namespace Usm.Shared.Patterns.Cache.Builders;

/// <summary>
/// Fluent builder for constructing a cache.
/// </summary>
/// <typeparam name="TKey">The cache key type.</typeparam>
/// <typeparam name="TValue">The cache value type.</typeparam>
public sealed class CacheBuilder<TKey, TValue> : ICacheBuilder<TKey, TValue>
    where TKey : notnull
{
    private readonly CacheOptions _options = new();
    private TimeProvider _timeProvider = TimeProvider.System;
    private CacheMetrics _metrics = new();

    /// <inheritdoc />
    public ICacheBuilder<TKey, TValue> UseLru()
    {
        _options.Policy = CacheEvictionPolicy.Lru;
        return this;
    }

    /// <inheritdoc />
    public ICacheBuilder<TKey, TValue> UseLfu()
    {
        _options.Policy = CacheEvictionPolicy.Lfu;
        return this;
    }

    /// <inheritdoc />
    public ICacheBuilder<TKey, TValue> WithCapacity(int capacity)
    {
        _options.Capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));
        return this;
    }

    /// <inheritdoc />
    public ICacheBuilder<TKey, TValue> WithDefaultExpiration(TimeSpan? expiration)
    {
        if (expiration is { } ttl && ttl <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(expiration));

        _options.DefaultExpiration = expiration;
        return this;
    }

    /// <inheritdoc />
    public ICacheBuilder<TKey, TValue> WithTimeProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        return this;
    }

    /// <inheritdoc />
    public ICacheBuilder<TKey, TValue> WithMetrics(CacheMetrics metrics)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        return this;
    }

    /// <inheritdoc />
    public ICache<TKey, TValue> Build()
        => new InMemoryCache<TKey, TValue>(_options, _timeProvider, _metrics);
}
