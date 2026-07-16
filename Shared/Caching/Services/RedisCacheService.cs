using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Usm.Shared.Caching.Abstractions;
using Usm.Shared.Caching.Internal;
using Usm.Shared.Caching.Keys;
using Usm.Shared.Caching.Models;
using Usm.Shared.Caching.Options;
using Usm.Shared.Caching.Serialization;

namespace Usm.Shared.Caching.Services;

internal sealed class RedisCacheService(
    IDistributedCache distributedCache,
    IMemoryCache memoryCache,
    ICacheSerializer serializer,
    ICacheKeyGenerator keyGenerator,
    IRedisConnectionAccessor redisConnectionAccessor,
    IOptions<RedisCachingOptions> options,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly IDistributedCache _distributedCache = distributedCache;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ICacheSerializer _serializer = serializer;
    private readonly ICacheKeyGenerator _keyGenerator = keyGenerator;
    private readonly IRedisConnectionAccessor _redisConnectionAccessor = redisConnectionAccessor;
    private readonly RedisCachingOptions _options = options.Value;
    private readonly ILogger<RedisCacheService> _logger = logger;
    private readonly Dictionary<string, SemaphoreSlim> _keyLocks = new(StringComparer.Ordinal);
    private readonly Lock _keyLockGuard = new();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var result = await TryGetValueAsync<T>(key, cancellationToken).ConfigureAwait(false);
        return result.Value;
    }

    public async Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeKey(key);
        var effectiveOptions = ResolveOptions(options);
        var payload = _serializer.Serialize(value);

        try
        {
            await _distributedCache.SetAsync(normalizedKey, payload, effectiveOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable when writing cache key {CacheKey}. Using in-memory fallback.", normalizedKey);
        }

        StoreInLocalFallback(normalizedKey, value, effectiveOptions);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeKey(key);
        _memoryCache.Remove(normalizedKey);

        try
        {
            await _distributedCache.RemoveAsync(normalizedKey, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable when removing cache key {CacheKey}.", normalizedKey);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var cachedResult = await TryGetValueAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (cachedResult.Found)
            return cachedResult.Value!;

        var semaphore = GetOrCreateKeySemaphore(key);
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cachedResult = await TryGetValueAsync<T>(key, cancellationToken).ConfigureAwait(false);
            if (cachedResult.Found)
                return cachedResult.Value!;

            var value = await factory(cancellationToken).ConfigureAwait(false);
            await SetAsync(key, value, options, cancellationToken).ConfigureAwait(false);
            return value;
        }
        finally
        {
            semaphore.Release();
            TryReleaseKeySemaphore(key, semaphore);
        }
    }

    public async Task<IReadOnlyDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var materializedKeys = keys.Distinct(StringComparer.Ordinal).ToArray();
        var results = new Dictionary<string, T?>(materializedKeys.Length, StringComparer.Ordinal);

        foreach (var key in materializedKeys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results[key] = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        }

        return results;
    }

    public async Task SetManyAsync<T>(
        IReadOnlyDictionary<string, T> entries,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var (key, value) in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SetAsync(key, value, options, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        foreach (var key in keys.Distinct(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var connection = await _redisConnectionAccessor.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (connection is null)
            {
                _logger.LogWarning("Pattern invalidation skipped because Redis connection is unavailable. Pattern: {Pattern}", pattern);
                return;
            }

            var database = connection.GetDatabase();
            var redisPattern = $"{_options.KeyPrefix}:{pattern}";
            var endpoints = connection.GetEndPoints();

            foreach (var endpoint in endpoints)
            {
                var server = connection.GetServer(endpoint);
                if (!server.IsConnected)
                    continue;

                await foreach (var redisKey in server.KeysAsync(database.Database, redisPattern).WithCancellation(cancellationToken))
                {
                    await database.KeyDeleteAsync(redisKey).ConfigureAwait(false);
                    _memoryCache.Remove(redisKey.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pattern invalidation failed for pattern {Pattern}.", pattern);
        }
    }

    private string NormalizeKey(string key)
    {
        var builtKey = _keyGenerator.Build(key);
        return _keyGenerator.Build(_options.KeyPrefix, builtKey);
    }

    private SemaphoreSlim GetOrCreateKeySemaphore(string key)
    {
        var normalizedKey = NormalizeKey(key);
        lock (_keyLockGuard)
        {
            if (_keyLocks.TryGetValue(normalizedKey, out var existing))
                return existing;

            var created = new SemaphoreSlim(1, 1);
            _keyLocks[normalizedKey] = created;
            return created;
        }
    }

    private void TryReleaseKeySemaphore(string key, SemaphoreSlim semaphore)
    {
        if (semaphore.CurrentCount == 0)
            return;

        var normalizedKey = NormalizeKey(key);
        lock (_keyLockGuard)
        {
            if (_keyLocks.TryGetValue(normalizedKey, out var current) && ReferenceEquals(current, semaphore))
                _keyLocks.Remove(normalizedKey);
        }
    }

    private async Task<(bool Found, T? Value)> TryGetValueAsync<T>(string key, CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizeKey(key);

        try
        {
            var payload = await _distributedCache.GetAsync(normalizedKey, cancellationToken).ConfigureAwait(false);
            if (payload is null || payload.Length == 0)
                return TryGetFromLocalFallback<T>(normalizedKey);

            var value = _serializer.Deserialize<T>(payload);
            StoreInLocalFallback(normalizedKey, value, ResolveOptions(null));
            return (true, value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable when reading cache key {CacheKey}. Falling back to in-memory cache.", normalizedKey);
            return TryGetFromLocalFallback<T>(normalizedKey);
        }
    }

    private (bool Found, T? Value) TryGetFromLocalFallback<T>(string normalizedKey)
    {
        if (_memoryCache.TryGetValue(normalizedKey, out LocalCacheValue<T>? localValue) && localValue is not null)
            return (localValue.HasValue, localValue.Value);

        return (false, default);
    }

    private void StoreInLocalFallback<T>(string normalizedKey, T? value, DistributedCacheEntryOptions options)
    {
        var memoryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
            SlidingExpiration = options.SlidingExpiration
        };

        _memoryCache.Set(normalizedKey, new LocalCacheValue<T>(true, value), memoryOptions);
    }

    private DistributedCacheEntryOptions ResolveOptions(CacheEntryOptions? options)
    {
        var effectiveAbsoluteSeconds = options?.AbsoluteExpirationRelativeToNow?.TotalSeconds
            ?? _options.DefaultAbsoluteExpirationSeconds;
        var effectiveSlidingSeconds = options?.SlidingExpiration?.TotalSeconds
            ?? _options.DefaultSlidingExpirationSeconds;

        var distributedOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(effectiveAbsoluteSeconds)
        };

        if (effectiveSlidingSeconds is > 0)
            distributedOptions.SlidingExpiration = TimeSpan.FromSeconds(effectiveSlidingSeconds.Value);

        return distributedOptions;
    }

    private sealed record LocalCacheValue<T>(bool HasValue, T? Value);
}
