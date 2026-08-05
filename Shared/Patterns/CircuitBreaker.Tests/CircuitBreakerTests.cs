using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.CircuitBreaker;
using Usm.Shared.Patterns.CircuitBreaker.Abstractions;
using Usm.Shared.Patterns.CircuitBreaker.Builders;
using Usm.Shared.Patterns.CircuitBreaker.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.CircuitBreaker.Tests;

public sealed class CircuitBreakerTests
{
    [Fact]
    public async Task OpensAfterFailureThreshold()
    {
        var time = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var breaker = new CircuitBreakerBuilder<string, string>()
            .WithFailureThreshold(2)
            .WithOpenDuration(TimeSpan.FromMinutes(1))
            .WithTimeProvider(time)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => breaker.ExecuteAsync("x", static (_, _) => throw new InvalidOperationException("boom")).AsTask());
        await Assert.ThrowsAsync<InvalidOperationException>(() => breaker.ExecuteAsync("x", static (_, _) => throw new InvalidOperationException("boom")).AsTask());
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => breaker.ExecuteAsync("x", static (_, _) => ValueTask.FromResult("ok")).AsTask());

        Assert.Equal(CircuitBreakerState.Open, breaker.State);
    }

    [Fact]
    public async Task ResetsFromHalfOpenAfterOpenDuration()
    {
        var time = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var breaker = new CircuitBreakerBuilder<string, string>()
            .WithFailureThreshold(1)
            .WithOpenDuration(TimeSpan.FromMinutes(1))
            .WithTimeProvider(time)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => breaker.ExecuteAsync("x", static (_, _) => throw new InvalidOperationException("boom")).AsTask());
        Assert.Equal(CircuitBreakerState.Open, breaker.State);

        time.Advance(TimeSpan.FromMinutes(2));
        var result = await breaker.ExecuteAsync("x", static (_, _) => ValueTask.FromResult("ok"));

        Assert.Equal("ok", result);
        Assert.Equal(CircuitBreakerState.Closed, breaker.State);
    }

    [Fact]
    public async Task EnforcesExecutionTimeout()
    {
        var breaker = new CircuitBreakerBuilder<string, string>()
            .WithExecutionTimeout(TimeSpan.FromMilliseconds(1))
            .Build();

        await Assert.ThrowsAsync<TimeoutException>(() => breaker.ExecuteAsync("x", async (_, token) =>
        {
            await Task.Delay(100, token);
            return "ok";
        }).AsTask());

        Assert.True(breaker.Options.ExecutionTimeout.HasValue);
    }

    [Fact]
    public void SupportsSynchronousExecution()
    {
        var breaker = new CircuitBreakerBuilder<int, int>()
            .WithFailureThreshold(2)
            .Build();

        var result = breaker.Execute(4, value => value * 2);

        Assert.Equal(8, result);
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddCircuitBreakerFramework();

        using var provider = services.BuildServiceProvider();
        var builder = provider.GetRequiredService<CircuitBreakerBuilder<string, string>>();

        Assert.NotNull(builder);
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
