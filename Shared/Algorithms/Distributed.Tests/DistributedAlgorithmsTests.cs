using Usm.Shared.Algorithms.Distributed.Abstractions;
using Usm.Shared.Algorithms.Distributed.Extensions;
using Xunit;

namespace Usm.Shared.Algorithms.Distributed.Tests;

public sealed class DistributedAlgorithmsTests
{
    [Fact]
    public void PerformsConsistentHashing()
    {
        var alg = DistributedAlgorithmsExtensions.CreateBuilder().Build();
        var hash1 = alg.ConsistentHash("key1", 100);
        var hash2 = alg.ConsistentHash("key1", 100);
        Assert.Equal(hash1, hash2);
        Assert.True(hash1 < 100);
    }

    [Fact]
    public void GeneratesSnowflakeIds()
    {
        var alg = DistributedAlgorithmsExtensions.CreateBuilder().Build();
        var id1 = alg.SnowflakeId();
        var id2 = alg.SnowflakeId();
        Assert.True(id1 < id2);
    }

    [Fact]
    public void IncrementsVectorClock()
    {
        var alg = DistributedAlgorithmsExtensions.CreateBuilder().Build();
        var clock = new Dictionary<int, long> { { 1, 0 } };
        alg.VectorClockIncrement(clock, 1);
        Assert.Equal(1, clock[1]);
    }

    [Fact]
    public void ComputesLamportClock()
    {
        var alg = DistributedAlgorithmsExtensions.CreateBuilder().Build();
        var clock = alg.LamportClockIncrement(5, 10);
        Assert.Equal(11, clock);
    }

    [Fact]
    public void TokenBucketRateLimiting()
    {
        var alg = DistributedAlgorithmsExtensions.CreateBuilder().Build();
        long tokens = 10;
        long lastRefill = 0;
        Assert.True(alg.TokenBucketAllow(ref tokens, ref lastRefill, 1.0, 10, 0));
        Assert.Equal(9, tokens);
    }

    [Fact]
    public void SlidingWindowRateLimiting()
    {
        var alg = DistributedAlgorithmsExtensions.CreateBuilder().Build();
        var window = new Deque<long>();
        Assert.True(alg.SlidingWindowAllow(window, 0, 2, 1000));
        Assert.True(alg.SlidingWindowAllow(window, 100, 2, 1000));
        Assert.False(alg.SlidingWindowAllow(window, 200, 2, 1000));
    }

    [Fact]
    public void ExponentialBackoff()
    {
        var alg = DistributedAlgorithmsExtensions.CreateBuilder().Build();
        var backoff = alg.ExponentialBackoffMs(0, 10_000);
        Assert.True(backoff >= 0 && backoff <= 10_000);
    }
}
