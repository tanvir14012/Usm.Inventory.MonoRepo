namespace Usm.Shared.Algorithms.Distributed.Abstractions;

/// <summary>
/// Represents distributed system algorithms.
/// </summary>
public interface IDistributedAlgorithms
{
    /// <summary>Gets hash of a key on a consistent ring.</summary>
    uint ConsistentHash(string key, uint ring);

    /// <summary>Gets rendezvous hash weight for a node.</summary>
    double RendezvousHash(string key, string node);

    /// <summary>Generates snowflake ID.</summary>
    long SnowflakeId();

    /// <summary>Increments vector clock.</summary>
    void VectorClockIncrement(IDictionary<int, long> clock, int processId);

    /// <summary>Gets Lamport clock value.</summary>
    long LamportClockIncrement(long current, long received);

    /// <summary>Token bucket rate limiter check.</summary>
    bool TokenBucketAllow(ref long tokens, ref long lastRefill, double rate, long capacity, long now);

    /// <summary>Sliding window rate limiter check.</summary>
    bool SlidingWindowAllow(Deque<long> window, long now, int limit, long windowMs);

    /// <summary>Leaky bucket rate limiter check.</summary>
    bool LeakyBucketAllow(ref long lastLeak, ref double level, double rate, long now);

    /// <summary>Computes exponential backoff with jitter.</summary>
    long ExponentialBackoffMs(int attempt, int maxMs);
}

/// <summary>Helper for sliding window implementation.</summary>
public sealed class Deque<T> where T : notnull
{
    private readonly List<T> _items = new();

    public int Count => _items.Count;

    public void PushFront(T item) => _items.Insert(0, item);

    public void PopBack()
    {
        if (_items.Count > 0)
            _items.RemoveAt(_items.Count - 1);
    }

    public T? PeekBack() => _items.Count > 0 ? _items[_items.Count - 1] : default;

    public void Clear() => _items.Clear();
}
