using System.Security.Cryptography;
using Usm.Shared.Algorithms.Distributed.Abstractions;

namespace Usm.Shared.Algorithms.Distributed.Implementation;

/// <summary>
/// Distributed system algorithms.
/// </summary>
public sealed class DistributedAlgorithms : IDistributedAlgorithms
{
    private static long _snowflakeSequence;
    private static readonly object _snowflakeLock = new();
    private const long Epoch = 1_672_531_200_000;

    /// <inheritdoc />
    public uint ConsistentHash(string key, uint ring)
    {
        ArgumentNullException.ThrowIfNull(key);
        var hash = FnvHash(key);
        return hash % ring;
    }

    /// <inheritdoc />
    public double RendezvousHash(string key, string node)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(node);
        var combined = $"{key}:{node}";
        return FnvHash(combined);
    }

    /// <inheritdoc />
    public long SnowflakeId()
    {
        lock (_snowflakeLock)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - Epoch;
            var id = (now << 22) | (1L << 12) | _snowflakeSequence;
            _snowflakeSequence = (_snowflakeSequence + 1) & 0xFFF;
            return id;
        }
    }

    /// <inheritdoc />
    public void VectorClockIncrement(IDictionary<int, long> clock, int processId)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (!clock.ContainsKey(processId))
            clock[processId] = 0;
        clock[processId]++;
    }

    /// <inheritdoc />
    public long LamportClockIncrement(long current, long received)
    {
        return Math.Max(current, received) + 1;
    }

    /// <inheritdoc />
    public bool TokenBucketAllow(ref long tokens, ref long lastRefill, double rate, long capacity, long now)
    {
        var elapsed = now - lastRefill;
        var generated = (long)(elapsed * rate / 1000.0);
        tokens = Math.Min(tokens + generated, capacity);
        lastRefill = now;

        if (tokens > 0)
        {
            tokens--;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool SlidingWindowAllow(Deque<long> window, long now, int limit, long windowMs)
    {
        ArgumentNullException.ThrowIfNull(window);

        while (window.PeekBack() != null && now - window.PeekBack()! > windowMs)
            window.PopBack();

        if (window.Count < limit)
        {
            window.PushFront(now);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool LeakyBucketAllow(ref long lastLeak, ref double level, double rate, long now)
    {
        var elapsed = now - lastLeak;
        level = Math.Max(0, level - (elapsed * rate / 1000.0));
        lastLeak = now;

        if (level < 1.0)
        {
            level += 1.0;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public long ExponentialBackoffMs(int attempt, int maxMs)
    {
        var backoff = Math.Min((long)Math.Pow(2, attempt) * 1000, maxMs);
        var jitter = Random.Shared.Next((int)backoff);
        return backoff / 2 + jitter;
    }

    private static uint FnvHash(string input)
    {
        const uint fnvPrime = 16_777_619;
        const uint fnvOffset = 2_166_136_261;

        uint hash = fnvOffset;
        foreach (var c in input)
        {
            hash ^= c;
            hash *= fnvPrime;
        }

        return hash;
    }
}
