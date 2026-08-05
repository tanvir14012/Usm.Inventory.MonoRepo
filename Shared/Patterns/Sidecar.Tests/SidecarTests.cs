using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.Sidecar.Abstractions;
using Usm.Shared.Patterns.Sidecar.Builders;
using Usm.Shared.Patterns.Sidecar.Extensions;
using Usm.Shared.Patterns.Sidecar.Models;
using Xunit;

namespace Usm.Shared.Patterns.Sidecar.Tests;

// ── Fake primary used across tests ────────────────────────────────────────────

public interface IWeatherService
{
    ValueTask<string> GetForecastAsync(CancellationToken cancellationToken = default);
}

public sealed class FailingWeatherService : IWeatherService
{
    private int _callCount;
    private readonly int _failUntil;
    private readonly Exception _exception;

    public FailingWeatherService(int failUntil, Exception? exception = null)
    {
        _failUntil = failUntil;
        _exception = exception ?? new InvalidOperationException("transient");
    }

    public int CallCount => _callCount;

    public ValueTask<string> GetForecastAsync(CancellationToken cancellationToken = default)
    {
        var count = Interlocked.Increment(ref _callCount);
        if (count <= _failUntil)
            throw _exception;

        return ValueTask.FromResult("sunny");
    }
}

public sealed class AlwaysFailingWeatherService : IWeatherService
{
    public ValueTask<string> GetForecastAsync(CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("always fails");
}

public sealed class SlowWeatherService : IWeatherService
{
    private readonly TimeSpan _delay;

    public SlowWeatherService(TimeSpan delay) => _delay = delay;

    public async ValueTask<string> GetForecastAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(_delay, cancellationToken);
        return "slow";
    }
}

// ── Test clock ────────────────────────────────────────────────────────────────

file sealed class MutableClock : TimeProvider
{
    private DateTimeOffset _now;

    public MutableClock(DateTimeOffset now) => _now = now;

    public void Advance(TimeSpan span) => _now = _now.Add(span);

    public override DateTimeOffset GetUtcNow() => _now;
    public override long GetTimestamp() => _now.Ticks;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        => throw new NotSupportedException();
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public sealed class SidecarTests
{
    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SuccessfulCall_ReturnsResult_AndIncrementsSuccessCounter()
    {
        var sidecar = new SidecarBuilder<IWeatherService>()
            .WithMaxAttempts(1)
            .Build(new FailingWeatherService(failUntil: 0));

        var result = await sidecar.ExecuteAsync(
            (svc, ct) => svc.GetForecastAsync(ct));

        Assert.Equal("sunny", result);
        Assert.Equal(1, sidecar.Metrics.Successes);
        Assert.Equal(1, sidecar.Metrics.TotalCalls);
        Assert.Equal(0, sidecar.Metrics.Failures);
    }

    // ── Exponential back-off retry ────────────────────────────────────────────

    [Fact]
    public async Task RetrySucceeds_AfterTransientFailures()
    {
        // Fails on first 2 attempts, succeeds on the 3rd
        var primary = new FailingWeatherService(failUntil: 2);
        var sidecar = new SidecarBuilder<IWeatherService>()
            .WithMaxAttempts(3)
            .WithRetryStrategy(SidecarRetryStrategy.Exponential)
            .WithRetryBaseDelay(TimeSpan.Zero)  // no actual delay in tests
            .WithJitter(false)
            .Build(primary);

        var result = await sidecar.ExecuteAsync(
            (svc, ct) => svc.GetForecastAsync(ct));

        Assert.Equal("sunny", result);
        Assert.Equal(2, sidecar.Metrics.Retries);  // 2 retries
        Assert.Equal(1, sidecar.Metrics.Successes);
    }

    [Fact]
    public async Task RetryExhausted_ThrowsLastException()
    {
        var primary = new AlwaysFailingWeatherService();
        var sidecar = new SidecarBuilder<IWeatherService>()
            .WithMaxAttempts(3)
            .WithRetryBaseDelay(TimeSpan.Zero)
            .WithJitter(false)
            .Build(primary);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sidecar.ExecuteAsync((svc, ct) => svc.GetForecastAsync(ct)).AsTask());

        Assert.Equal(1, sidecar.Metrics.Failures);
        Assert.Equal(2, sidecar.Metrics.Retries);
    }

    // ── Circuit breaker ───────────────────────────────────────────────────────

    [Fact]
    public async Task CircuitBreaker_Opens_AfterFailureThreshold()
    {
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        var primary = new AlwaysFailingWeatherService();
        var sidecar = new SidecarBuilder<IWeatherService>()
            .WithMaxAttempts(1)             // no retry, each call = 1 failure
            .WithFailureThreshold(3)
            .WithCircuitOpenDuration(TimeSpan.FromMinutes(1))
            .WithRetryBaseDelay(TimeSpan.Zero)
            .WithJitter(false)
            .WithTimeProvider(clock)
            .Build(primary);

        // Three failures → circuit trips
        for (var i = 0; i < 3; i++)
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sidecar.ExecuteAsync((svc, ct) => svc.GetForecastAsync(ct)).AsTask());

        Assert.Equal(SidecarCircuitState.Open, sidecar.CircuitState);

        // Next call is rejected immediately
        await Assert.ThrowsAsync<SidecarCircuitOpenException>(() =>
            sidecar.ExecuteAsync((svc, ct) => svc.GetForecastAsync(ct)).AsTask());

        Assert.Equal(1, sidecar.Metrics.RejectedByCircuit);
        Assert.Equal(1, sidecar.Metrics.CircuitTrips);
    }

    [Fact]
    public async Task CircuitBreaker_HalfOpen_ThenCloses_OnSuccessfulProbe()
    {
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        var primary = new FailingWeatherService(failUntil: 1);

        var sidecar = new SidecarBuilder<IWeatherService>()
            .WithMaxAttempts(1)
            .WithFailureThreshold(1)
            .WithCircuitOpenDuration(TimeSpan.FromSeconds(10))
            .WithRetryBaseDelay(TimeSpan.Zero)
            .WithJitter(false)
            .WithTimeProvider(clock)
            .Build(primary);

        // Trip the circuit
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sidecar.ExecuteAsync((svc, ct) => svc.GetForecastAsync(ct)).AsTask());

        Assert.Equal(SidecarCircuitState.Open, sidecar.CircuitState);

        // Advance past open duration → half-open
        clock.Advance(TimeSpan.FromSeconds(11));

        // Successful probe → circuit closes
        var result = await sidecar.ExecuteAsync((svc, ct) => svc.GetForecastAsync(ct));

        Assert.Equal("sunny", result);
        Assert.Equal(SidecarCircuitState.Closed, sidecar.CircuitState);
        Assert.Equal(1, sidecar.Metrics.CircuitResets);
    }

    // ── Execution timeout ─────────────────────────────────────────────────────

    [Fact]
    public async Task ExecutionTimeout_ThrowsTimeoutException()
    {
        var primary = new SlowWeatherService(TimeSpan.FromSeconds(10));
        var sidecar = new SidecarBuilder<IWeatherService>()
            .WithMaxAttempts(1)
            .WithExecutionTimeout(TimeSpan.FromMilliseconds(10))
            .WithRetryBaseDelay(TimeSpan.Zero)
            .WithJitter(false)
            .Build(primary);

        await Assert.ThrowsAsync<TimeoutException>(() =>
            sidecar.ExecuteAsync((svc, ct) => svc.GetForecastAsync(ct)).AsTask());

        Assert.Equal(1, sidecar.Metrics.Timeouts);
    }

    // ── Fire-and-forget overload ───────────────────────────────────────────────

    [Fact]
    public async Task VoidExecuteAsync_Succeeds()
    {
        var executed = false;
        var sidecar = new SidecarBuilder<IWeatherService>()
            .Build(new FailingWeatherService(failUntil: 0));

        await sidecar.ExecuteAsync(async (svc, ct) =>
        {
            _ = await svc.GetForecastAsync(ct);
            executed = true;
        });

        Assert.True(executed);
    }

    // ── Metrics snapshot ──────────────────────────────────────────────────────

    [Fact]
    public async Task Metrics_Snapshot_ReflectsAllCounters()
    {
        var primary = new FailingWeatherService(failUntil: 1);
        var sidecar = new SidecarBuilder<IWeatherService>()
            .WithMaxAttempts(2)
            .WithRetryBaseDelay(TimeSpan.Zero)
            .WithJitter(false)
            .Build(primary);

        await sidecar.ExecuteAsync((svc, ct) => svc.GetForecastAsync(ct));

        var snap = sidecar.Metrics.Snapshot(sidecar.CircuitState);

        Assert.Equal(1, snap.TotalCalls);
        Assert.Equal(1, snap.Successes);
        Assert.Equal(1, snap.Retries);
        Assert.Equal(0, snap.Failures);
        Assert.Equal(SidecarCircuitState.Closed, snap.CircuitState);
    }

    // ── Fluent builder validation ─────────────────────────────────────────────

    [Fact]
    public void Builder_Rejects_InvalidMaxAttempts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SidecarBuilder<IWeatherService>().WithMaxAttempts(0));
    }

    [Fact]
    public void Builder_Rejects_NonPositiveFailureThreshold()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SidecarBuilder<IWeatherService>().WithFailureThreshold(0));
    }

    // ── DI registration ───────────────────────────────────────────────────────

    [Fact]
    public void DI_RegistersBuilderAndSidecar()
    {
        var services = new ServiceCollection();
        services.AddSidecarFramework();

        using var provider = services.BuildServiceProvider();

        var builder = provider.GetRequiredService<ISidecarBuilder<IWeatherService>>();
        Assert.NotNull(builder);
    }

    [Fact]
    public void AddSidecar_RegistersSidecarAsSingleton()
    {
        var services = new ServiceCollection();
        services
            .AddSidecarFramework()
            .AddSidecar<IWeatherService, ConcreteWeatherService>(
                configure: o => o.MaxAttempts = 2);

        using var provider = services.BuildServiceProvider();
        var sidecar = provider.GetRequiredService<ISidecar<IWeatherService>>();

        Assert.NotNull(sidecar);
        Assert.Equal(2, sidecar.Options.MaxAttempts);
    }

    private sealed class ConcreteWeatherService : IWeatherService
    {
        public ValueTask<string> GetForecastAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult("clear");
    }
}
