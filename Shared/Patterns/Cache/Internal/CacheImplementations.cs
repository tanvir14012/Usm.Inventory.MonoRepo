using System.Collections.Concurrent;
using Usm.Shared.Patterns.Cache.Abstractions;
using Usm.Shared.Patterns.Cache.Builders;
using Usm.Shared.Patterns.Cache.Configuration;

namespace Usm.Shared.Patterns.Cache;

/// <summary>
/// Base type for reusable caches.
/// </summary>
/// <typeparam name="TKey">The cache key type.</typeparam>
/// <typeparam name="TValue">The cache value type.</typeparam>
public abstract class Cache<TKey, TValue> : ICache<TKey, TValue>
    where TKey : notnull
{
    /// <summary>Creates a builder for a cache.</summary>
    public static CacheBuilder<TKey, TValue> CreateBuilder()
        => new();

    /// <inheritdoc />
    public abstract int Count { get; }

    /// <inheritdoc />
    public abstract CacheMetricsSnapshot Metrics { get; }

    /// <inheritdoc />
    public abstract bool TryGetValue(TKey key, out TValue? value);

    /// <inheritdoc />
    public abstract ValueTask<(bool Found, TValue? Value)> TryGetValueAsync(TKey key, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract ValueTask SetAsync(TKey key, TValue value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract ValueTask<TValue> GetOrCreateAsync(
        TKey key,
        Func<CancellationToken, ValueTask<TValue>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract ValueTask ClearAsync(CancellationToken cancellationToken = default);
}

internal sealed class CacheEntry<TKey, TValue>
    where TKey : notnull
{
    public CacheEntry(TValue value, DateTimeOffset? expiresAt)
    {
        Value = value;
        ExpiresAt = expiresAt;
    }

    public TValue Value { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public int Frequency { get; set; } = 1;
    public LinkedListNode<TKey>? Node { get; set; }
}

/// <summary>
/// In-memory cache implementation with LRU or LFU eviction.
/// </summary>
/// <typeparam name="TKey">The cache key type.</typeparam>
/// <typeparam name="TValue">The cache value type.</typeparam>
internal sealed class InMemoryCache<TKey, TValue> : Cache<TKey, TValue>
    where TKey : notnull
{
    private readonly CacheOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly CacheMetrics _metrics;
    private readonly Dictionary<TKey, CacheEntry<TKey, TValue>> _entries = new();
    private readonly Dictionary<TKey, SemaphoreSlim> _keyLocks = new();
    private readonly LinkedList<TKey> _lru = new();
    private readonly Dictionary<int, LinkedList<TKey>> _lfuBuckets = new();
    private readonly object _gate = new();

    public InMemoryCache(CacheOptions options, TimeProvider timeProvider, CacheMetrics metrics)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public override int Count
    {
        get
        {
            lock (_gate)
                return _entries.Count;
        }
    }

    public override CacheMetricsSnapshot Metrics => _metrics.Snapshot();

    public override bool TryGetValue(TKey key, out TValue? value)
    {
        lock (_gate)
        {
            if (!TryGetValueCore(key, out value))
                return false;

            _metrics.RecordHit();
            TouchEntry(key);
            return true;
        }
    }

    public override ValueTask<(bool Found, TValue? Value)> TryGetValueAsync(TKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (TryGetValueCore(key, out var value))
            {
                _metrics.RecordHit();
                TouchEntry(key);
                return ValueTask.FromResult((true, value));
            }
            return ValueTask.FromResult((false, default(TValue)));
        }
    }

    public override ValueTask SetAsync(TKey key, TValue value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            SetCore(key, value, options);
            return ValueTask.CompletedTask;
        }
    }

    public override async ValueTask<TValue> GetOrCreateAsync(
        TKey key,
        Func<CancellationToken, ValueTask<TValue>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (TryGetValue(key, out var cached) && cached is not null)
            return cached;

        var semaphore = GetOrCreateSemaphore(key);
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (TryGetValue(key, out cached) && cached is not null)
                return cached;

            var created = await factory(cancellationToken).ConfigureAwait(false);
            await SetAsync(key, created, options, cancellationToken).ConfigureAwait(false);
            return created;
        }
        finally
        {
            semaphore.Release();
            CleanupSemaphore(key, semaphore);
        }
    }

    public override ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            RemoveCore(key, countAsEviction: false);
            return ValueTask.CompletedTask;
        }
    }

    public override ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _entries.Clear();
            _lru.Clear();
            _lfuBuckets.Clear();
            _keyLocks.Clear();
            return ValueTask.CompletedTask;
        }
    }

    private bool TryGetValueCore(TKey key, out TValue? value)
    {
        if (!_entries.TryGetValue(key, out var entry))
        {
            value = default;
            _metrics.RecordMiss();
            return false;
        }

        if (IsExpired(entry))
        {
            RemoveCore(key, countAsEviction: false, expired: true);
            value = default;
            _metrics.RecordMiss();
            return false;
        }

        value = entry.Value;
        return true;
    }

    private void SetCore(TKey key, TValue value, CacheEntryOptions? options)
    {
        ExpireEntries();

        var expiresAt = ResolveExpiration(options);
        if (_entries.TryGetValue(key, out var existing))
        {
            existing.Value = value;
            existing.ExpiresAt = expiresAt;
            existing.Frequency++;
            TouchEntry(key);
            return;
        }

        if (_entries.Count >= _options.Capacity)
            EvictOne();

        var entry = new CacheEntry<TKey, TValue>(value, expiresAt);
        _entries[key] = entry;
        AddToAccessStructures(key, entry);
    }

    private void EvictOne()
    {
        switch (_options.Policy)
        {
            case CacheEvictionPolicy.Lfu:
                EvictLfu();
                break;
            default:
                EvictLru();
                break;
        }
    }

    private void EvictLru()
    {
        var node = _lru.Last;
        if (node is null)
            return;

        RemoveCore(node.Value, countAsEviction: true);
    }

    private void EvictLfu()
    {
        if (_lfuBuckets.Count == 0)
            return;

        var minFrequency = _lfuBuckets.Keys.Min();
        var bucket = _lfuBuckets[minFrequency];
        var node = bucket.Last;
        if (node is null)
            return;

        RemoveCore(node.Value, countAsEviction: true);
    }

    private void RemoveCore(TKey key, bool countAsEviction, bool expired = false)
    {
        if (!_entries.Remove(key, out var entry))
            return;

        RemoveFromAccessStructures(key, entry);

        if (expired)
            _metrics.RecordExpiration();
        else if (countAsEviction)
            _metrics.RecordEviction();
    }

    private void ExpireEntries()
    {
        var now = _timeProvider.GetUtcNow();
        List<TKey>? expiredKeys = null;

        foreach (var pair in _entries)
        {
            if (pair.Value.ExpiresAt is { } expiresAt && expiresAt <= now)
            {
                expiredKeys ??= new List<TKey>();
                expiredKeys.Add(pair.Key);
            }
        }

        if (expiredKeys is null)
            return;

        foreach (var key in expiredKeys)
            RemoveCore(key, countAsEviction: false, expired: true);
    }

    private bool IsExpired(CacheEntry<TKey, TValue> entry)
        => entry.ExpiresAt is { } expiresAt && expiresAt <= _timeProvider.GetUtcNow();

    private DateTimeOffset? ResolveExpiration(CacheEntryOptions? options)
    {
        var ttl = options?.AbsoluteExpirationRelativeToNow ?? _options.DefaultExpiration;
        return ttl is null ? null : _timeProvider.GetUtcNow().Add(ttl.Value);
    }

    private void AddToAccessStructures(TKey key, CacheEntry<TKey, TValue> entry)
    {
        if (_options.Policy == CacheEvictionPolicy.Lfu)
        {
            if (!_lfuBuckets.TryGetValue(entry.Frequency, out var bucket))
            {
                bucket = new LinkedList<TKey>();
                _lfuBuckets[entry.Frequency] = bucket;
            }

            entry.Node = bucket.AddFirst(key);
            return;
        }

        entry.Node = _lru.AddFirst(key);
    }

    private void TouchEntry(TKey key)
    {
        if (!_entries.TryGetValue(key, out var entry))
            return;

        if (_options.Policy == CacheEvictionPolicy.Lfu)
        {
            var oldFrequency = entry.Frequency;
            if (_lfuBuckets.TryGetValue(oldFrequency, out var oldBucket) && entry.Node is not null)
                oldBucket.Remove(entry.Node);

            entry.Frequency++;
            if (!_lfuBuckets.TryGetValue(entry.Frequency, out var bucket))
            {
                bucket = new LinkedList<TKey>();
                _lfuBuckets[entry.Frequency] = bucket;
            }

            entry.Node = bucket.AddFirst(key);
            if (oldBucket is { Count: 0 })
                _lfuBuckets.Remove(oldFrequency);
            return;
        }

        if (entry.Node is not null)
        {
            _lru.Remove(entry.Node);
            entry.Node = _lru.AddFirst(key);
        }
    }

    private void RemoveFromAccessStructures(TKey key, CacheEntry<TKey, TValue> entry)
    {
        if (_options.Policy == CacheEvictionPolicy.Lfu)
        {
            if (_lfuBuckets.TryGetValue(entry.Frequency, out var bucket) && entry.Node is not null)
            {
                bucket.Remove(entry.Node);
                if (bucket.Count == 0)
                    _lfuBuckets.Remove(entry.Frequency);
            }

            return;
        }

        if (entry.Node is not null)
            _lru.Remove(entry.Node);
    }

    private SemaphoreSlim GetOrCreateSemaphore(TKey key)
    {
        lock (_gate)
        {
            if (_keyLocks.TryGetValue(key, out var semaphore))
                return semaphore;

            semaphore = new SemaphoreSlim(1, 1);
            _keyLocks[key] = semaphore;
            return semaphore;
        }
    }

    private void CleanupSemaphore(TKey key, SemaphoreSlim semaphore)
    {
        lock (_gate)
        {
            if (_keyLocks.TryGetValue(key, out var current) && ReferenceEquals(current, semaphore))
                _keyLocks.Remove(key);
        }
    }
}

/// <summary>
/// Thread-safe cache metrics collector.
/// </summary>
public sealed class CacheMetrics : ICacheMetrics
{
    private long _hits;
    private long _misses;
    private long _evictions;
    private long _expirations;

    /// <inheritdoc />
    public long Hits => Interlocked.Read(ref _hits);

    /// <inheritdoc />
    public long Misses => Interlocked.Read(ref _misses);

    /// <inheritdoc />
    public long Evictions => Interlocked.Read(ref _evictions);

    /// <inheritdoc />
    public long Expirations => Interlocked.Read(ref _expirations);

    /// <inheritdoc />
    public CacheMetricsSnapshot Snapshot()
        => new(Hits, Misses, Evictions, Expirations);

    internal void RecordHit() => Interlocked.Increment(ref _hits);

    internal void RecordMiss() => Interlocked.Increment(ref _misses);

    internal void RecordEviction() => Interlocked.Increment(ref _evictions);

    internal void RecordExpiration() => Interlocked.Increment(ref _expirations);
}
