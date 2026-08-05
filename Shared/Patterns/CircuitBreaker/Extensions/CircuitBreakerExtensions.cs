using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.CircuitBreaker;
using Usm.Shared.Patterns.CircuitBreaker.Abstractions;
using Usm.Shared.Patterns.CircuitBreaker.Builders;

namespace Usm.Shared.Patterns.CircuitBreaker.Extensions;

/// <summary>
/// Common extension methods for circuit breaker creation and DI registration.
/// </summary>
public static class CircuitBreakerExtensions
{
    /// <summary>Registers the circuit breaker framework with dependency injection.</summary>
    public static IServiceCollection AddCircuitBreakerFramework(this IServiceCollection services)
    {
        services.AddOptions<CircuitBreakerOptions>();
        services.TryAddTransient(typeof(CircuitBreakerBuilder<,>), typeof(CircuitBreakerBuilder<,>));
        services.TryAddSingleton(typeof(ICircuitBreakerPolicy<,>), typeof(CircuitBreakerPolicy<,>));
        return services;
    }
}

/// <summary>
/// Default reusable circuit breaker policy.
/// </summary>
/// <typeparam name="TContext">The operation context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public sealed class CircuitBreakerPolicy<TContext, TResult> : ICircuitBreakerPolicy<TContext, TResult>
{
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger<CircuitBreakerPolicy<TContext, TResult>> _logger;
    private readonly object _gate = new();
    private readonly SemaphoreSlim _halfOpenGate;
    private CircuitBreakerState _state;
    private int _failureCount;
    private DateTimeOffset _openUntil;
    private readonly CircuitBreakerMetrics _metrics = new();

    /// <summary>Initializes a new circuit breaker policy.</summary>
    public CircuitBreakerPolicy(IOptions<CircuitBreakerOptions> options, ILogger<CircuitBreakerPolicy<TContext, TResult>>? logger = null)
    {
        _options = options.Value;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CircuitBreakerPolicy<TContext, TResult>>.Instance;
        _halfOpenGate = new SemaphoreSlim(Math.Max(1, _options.HalfOpenPermits), Math.Max(1, _options.HalfOpenPermits));
    }

    /// <inheritdoc />
    public CircuitBreakerState State
    {
        get
        {
            lock (_gate)
            {
                RefreshState();
                return _state;
            }
        }
    }

    /// <inheritdoc />
    public CircuitBreakerOptions Options => _options;

    /// <inheritdoc />
    public ICircuitBreakerMetrics Metrics => _metrics;

    /// <inheritdoc />
    public TResult Execute(TContext context, Func<TContext, TResult> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        lock (_gate)
        {
            RefreshState();
            if (_state == CircuitBreakerState.Open)
                throw new CircuitBreakerOpenException("The circuit breaker is open.");
        }

        var acquiredHalfOpenPermit = false;
        if (_state == CircuitBreakerState.HalfOpen)
        {
            acquiredHalfOpenPermit = _halfOpenGate.Wait(0);
            if (!acquiredHalfOpenPermit)
                throw new CircuitBreakerOpenException("The circuit breaker is half-open and has no available trial permits.");
        }

        try
        {
            if (_options.ExecutionTimeout is null)
            {
                var result = operation(context);
                OnSuccess();
                return result;
            }

            var task = Task.Run(() => operation(context));
            var resultWithTimeout = task.WaitAsync(_options.ExecutionTimeout.Value, CancellationToken.None).GetAwaiter().GetResult();
            OnSuccess();
            return resultWithTimeout;
        }
        catch (TimeoutException ex)
        {
            _metrics.RecordTimeout();
            OnFailure(ex);
            throw;
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            throw;
        }
        finally
        {
            if (acquiredHalfOpenPermit)
                _halfOpenGate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask<TResult> ExecuteAsync(
        TContext context,
        Func<TContext, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await EnsureCallAllowedAsync(cancellationToken).ConfigureAwait(false);
        var acquiredHalfOpenPermit = false;
        if (_state == CircuitBreakerState.HalfOpen)
        {
            acquiredHalfOpenPermit = await _halfOpenGate.WaitAsync(0, cancellationToken).ConfigureAwait(false);
            if (!acquiredHalfOpenPermit)
                throw new CircuitBreakerOpenException("The circuit breaker is half-open and has no available trial permits.");
        }

        try
        {
            var result = await ExecuteWithTimeoutAsync(operation, context, cancellationToken).ConfigureAwait(false);
            OnSuccess();
            return result;
        }
        catch (TimeoutException ex)
        {
            _metrics.RecordTimeout();
            OnFailure(ex);
            throw;
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            throw;
        }
        finally
        {
            if (acquiredHalfOpenPermit)
                _halfOpenGate.Release();
        }
    }

    private async ValueTask<TResult> ExecuteWithTimeoutAsync(
        Func<TContext, CancellationToken, ValueTask<TResult>> operation,
        TContext context,
        CancellationToken cancellationToken)
    {
        if (_options.ExecutionTimeout is null)
            return await operation(context, cancellationToken).ConfigureAwait(false);

        var task = operation(context, cancellationToken).AsTask();
        return await task.WaitAsync(_options.ExecutionTimeout.Value, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask EnsureCallAllowedAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            RefreshState();
            if (_state == CircuitBreakerState.Open)
                throw new CircuitBreakerOpenException("The circuit breaker is open.");
        }

        await ValueTask.CompletedTask;
    }

    private void RefreshState()
    {
        if (_state != CircuitBreakerState.Open)
            return;

        if (_options.TimeProvider.GetUtcNow() >= _openUntil)
        {
            _state = CircuitBreakerState.HalfOpen;
            _failureCount = 0;
            _logger.LogDebug("Circuit breaker transitioned to half-open.");
        }
    }

    private void OnSuccess()
    {
        lock (_gate)
        {
            if (_state == CircuitBreakerState.HalfOpen)
                _metrics.RecordReset();

            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _logger.LogDebug("Circuit breaker transitioned to closed.");
        }
    }

    private void OnFailure(Exception exception)
    {
        lock (_gate)
        {
            _metrics.RecordFailure();
            _failureCount++;

            if (_failureCount < _options.FailureThreshold && _state != CircuitBreakerState.HalfOpen)
                return;

            _state = CircuitBreakerState.Open;
            _openUntil = _options.TimeProvider.GetUtcNow().Add(_options.OpenDuration);
            _failureCount = 0;
            _logger.LogWarning(exception, "Circuit breaker opened until {OpenUntil}.", _openUntil);
            _metrics.RecordTrip();
        }
    }
}

/// <summary>
/// Exception thrown when a circuit breaker rejects a call.
/// </summary>
public sealed class CircuitBreakerOpenException : InvalidOperationException
{
    /// <summary>Initializes a new exception.</summary>
    public CircuitBreakerOpenException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thread-safe circuit breaker metrics collector.
/// </summary>
public sealed class CircuitBreakerMetrics : ICircuitBreakerMetrics
{
    private long _failures;
    private long _trips;
    private long _resets;
    private long _timeouts;

    /// <inheritdoc />
    public long Failures => Interlocked.Read(ref _failures);

    /// <inheritdoc />
    public long Trips => Interlocked.Read(ref _trips);

    /// <inheritdoc />
    public long Resets => Interlocked.Read(ref _resets);

    /// <inheritdoc />
    public long Timeouts => Interlocked.Read(ref _timeouts);

    /// <inheritdoc />
    public CircuitBreakerMetricsSnapshot Snapshot()
        => new(Failures, Trips, Resets, Timeouts);

    internal void RecordFailure() => Interlocked.Increment(ref _failures);

    internal void RecordTrip() => Interlocked.Increment(ref _trips);

    internal void RecordReset() => Interlocked.Increment(ref _resets);

    internal void RecordTimeout() => Interlocked.Increment(ref _timeouts);
}
