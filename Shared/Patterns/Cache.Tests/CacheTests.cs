using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.Cache.Abstractions;
using Usm.Shared.Patterns.Cache.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.Cache.Tests;

public sealed class CacheTests
{
    [Fact]
    public async Task EvictsLeastRecentlyUsedEntry()
    {
        var cache = Cache<string, string>.CreateBuilder()
            .UseLru()
            .WithCapacity(2)
            .Build();

        await cache.SetAsync("a", "1");
        await cache.SetAsync("b", "2");
        _ = await cache.TryGetValueAsync("a");
        await cache.SetAsync("c", "3");

        Assert.True(cache.TryGetValue("a", out _));
        Assert.False(cache.TryGetValue("b", out _));
        Assert.True(cache.TryGetValue("c", out _));
    }

    [Fact]
    public async Task EvictsLeastFrequentlyUsedEntry()
    {
        var cache = Cache<string, string>.CreateBuilder()
            .UseLfu()
            .WithCapacity(2)
            .Build();

        await cache.SetAsync("a", "1");
        await cache.SetAsync("b", "2");
        _ = await cache.TryGetValueAsync("a");
        _ = await cache.TryGetValueAsync("a");
        await cache.SetAsync("c", "3");

        Assert.True(cache.TryGetValue("a", out _));
        Assert.False(cache.TryGetValue("b", out _));
        Assert.True(cache.TryGetValue("c", out _));
    }

    [Fact]
    public async Task ExpiresEntriesUsingTimeProvider()
    {
        var time = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var cache = Cache<string, string>.CreateBuilder()
            .WithDefaultExpiration(TimeSpan.FromMinutes(1))
            .WithTimeProvider(time)
            .Build();

        await cache.SetAsync("a", "1");
        Assert.True(cache.TryGetValue("a", out _));

        time.Advance(TimeSpan.FromMinutes(2));

        Assert.False(cache.TryGetValue("a", out _));
        Assert.Equal(1, cache.Metrics.Expirations);
    }

    [Fact]
    public async Task GetOrCreateCachesFactoryResult()
    {
        var calls = 0;
        var cache = Cache<string, string>.CreateBuilder().Build();

        var value = await cache.GetOrCreateAsync("a", async token =>
        {
            await Task.Delay(1, token);
            calls++;
            return "1";
        });

        var cached = await cache.GetOrCreateAsync("a", static _ => ValueTask.FromResult("2"));

        Assert.Equal("1", value);
        Assert.Equal("1", cached);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddCacheFramework();

        using var provider = services.BuildServiceProvider();
        var metrics = provider.GetRequiredService<CacheMetrics>();
        var cache = Cache<string, string>.CreateBuilder()
            .WithMetrics(metrics)
            .Build();

        Assert.NotNull(cache);
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public MutableTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public void Advance(TimeSpan span) => _utcNow = _utcNow.Add(span);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public override long GetTimestamp() => DateTimeOffset.UtcNow.Ticks;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
            => throw new NotSupportedException();
    }
}
