using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.RateLimiter.Abstractions;
using Usm.Shared.Patterns.RateLimiter.Builders;
using Usm.Shared.Patterns.RateLimiter.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.RateLimiter.Tests;

public sealed class RateLimiterTests
{
    [Fact]
    public async Task TokenBucketRefillsOverTime()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UtcNow);
        var limiter = new RateLimiterBuilder<string>()
            .WithAlgorithm(RateLimiterAlgorithm.TokenBucket)
            .WithPermitLimit(2)
            .WithWindow(TimeSpan.FromSeconds(10))
            .WithTimeProvider(time)
            .Build();

        Assert.True((await limiter.AcquireAsync("a")).IsAcquired);
        Assert.True((await limiter.AcquireAsync("a")).IsAcquired);

        var rejected = await limiter.AcquireAsync("a");
        Assert.False(rejected.IsAcquired);

        time.Advance(TimeSpan.FromSeconds(10));
        Assert.True((await limiter.AcquireAsync("a")).IsAcquired);
    }

    [Fact]
    public async Task FixedWindowRejectsOverflow()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UtcNow);
        var limiter = new RateLimiterBuilder<string>()
            .WithAlgorithm(RateLimiterAlgorithm.FixedWindow)
            .WithPermitLimit(1)
            .WithWindow(TimeSpan.FromMinutes(1))
            .WithTimeProvider(time)
            .Build();

        Assert.True((await limiter.AcquireAsync("a")).IsAcquired);
        Assert.False((await limiter.AcquireAsync("a")).IsAcquired);
    }

    [Fact]
    public async Task SlidingWindowCountsAcrossSegments()
    {
        var time = new ManualTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var limiter = new RateLimiterBuilder<string>()
            .WithAlgorithm(RateLimiterAlgorithm.SlidingWindow)
            .WithPermitLimit(2)
            .WithWindow(TimeSpan.FromSeconds(8))
            .WithSegments(4)
            .WithTimeProvider(time)
            .Build();

        Assert.True((await limiter.AcquireAsync("a")).IsAcquired);
        Assert.True((await limiter.AcquireAsync("a")).IsAcquired);
        Assert.False((await limiter.AcquireAsync("a")).IsAcquired);

        time.Advance(TimeSpan.FromSeconds(8));
        Assert.True((await limiter.AcquireAsync("a")).IsAcquired);
    }

    [Fact]
    public async Task LeakyBucketLimitsQueueDepth()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UtcNow);
        var limiter = new RateLimiterBuilder<string>()
            .WithAlgorithm(RateLimiterAlgorithm.LeakyBucket)
            .WithQueueLimit(1)
            .WithWindow(TimeSpan.FromSeconds(10))
            .WithTimeProvider(time)
            .Build();

        Assert.True((await limiter.AcquireAsync("a")).IsAcquired);
        Assert.False((await limiter.AcquireAsync("a")).IsAcquired);

        time.Advance(TimeSpan.FromSeconds(10));
        Assert.True((await limiter.AcquireAsync("a")).IsAcquired);
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddRateLimiterFramework();

        using var provider = services.BuildServiceProvider();
        var builder = provider.GetRequiredService<IRateLimiterBuilder<string>>();

        Assert.NotNull(builder);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;

        public ManualTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan amount) => _now = _now.Add(amount);
    }
}
