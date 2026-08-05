using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.Sidecar.Abstractions;
using Usm.Shared.Patterns.Sidecar.Models;

namespace Usm.Shared.Patterns.Sidecar.Extensions;

/// <summary>
/// Exception thrown when a call is rejected because the sidecar's circuit is open.
/// </summary>
public sealed class SidecarCircuitOpenException : InvalidOperationException
{
    /// <summary>Initializes a new instance.</summary>
    public SidecarCircuitOpenException(string message) : base(message) { }
}

/// <summary>
/// Thread-safe metrics collector for a sidecar instance.
/// </summary>
public sealed class SidecarMetrics : ISidecarMetrics
{
    private long _totalCalls;
    private long _successes;
    private long _failures;
    private long _retries;
    private long _timeouts;
    private long _circuitTrips;
    private long _circuitResets;
    private long _rejectedByCircuit;

    /// <inheritdoc />
    public long TotalCalls => Interlocked.Read(ref _totalCalls);

    /// <inheritdoc />
    public long Successes => Interlocked.Read(ref _successes);

    /// <inheritdoc />
    public long Failures => Interlocked.Read(ref _failures);

    /// <inheritdoc />
    public long Retries => Interlocked.Read(ref _retries);

    /// <inheritdoc />
    public long Timeouts => Interlocked.Read(ref _timeouts);

    /// <inheritdoc />
    public long CircuitTrips => Interlocked.Read(ref _circuitTrips);

    /// <inheritdoc />
    public long CircuitResets => Interlocked.Read(ref _circuitResets);

    /// <inheritdoc />
    public long RejectedByCircuit => Interlocked.Read(ref _rejectedByCircuit);

    /// <inheritdoc />
    public SidecarMetricsSnapshot Snapshot(SidecarCircuitState currentState)
        => new(TotalCalls, Successes, Failures, Retries, Timeouts,
               CircuitTrips, CircuitResets, RejectedByCircuit, currentState);

    internal void RecordCall() => Interlocked.Increment(ref _totalCalls);
    internal void RecordSuccess() => Interlocked.Increment(ref _successes);
    internal void RecordFailure() => Interlocked.Increment(ref _failures);
    internal void RecordRetry() => Interlocked.Increment(ref _retries);
    internal void RecordTimeout() => Interlocked.Increment(ref _timeouts);
    internal void RecordCircuitTrip() => Interlocked.Increment(ref _circuitTrips);
    internal void RecordCircuitReset() => Interlocked.Increment(ref _circuitResets);
    internal void RecordRejectedByCircuit() => Interlocked.Increment(ref _rejectedByCircuit);
}

/// <summary>
/// Production-grade sidecar that wraps a primary service and transparently applies:
/// <list type="bullet">
///   <item>Exponential back-off retry with decorrelated jitter</item>
///   <item>Circuit breaker (closed → open → half-open → closed)</item>
///   <item>Per-call execution timeout</item>
///   <item>Structured telemetry</item>
/// </list>
/// </summary>
/// <typeparam name="TService">The primary service contract.</typeparam>
public sealed class Sidecar<TService> : ISidecar<TService> where TService : class
{
    private readonly SidecarOptions _options;
    private readonly ILogger<Sidecar<TService>> _logger;
    private readonly SidecarMetrics _metrics = new();
    private readonly SemaphoreSlim _halfOpenGate;

    // Circuit breaker state — all mutations under _gate
    private readonly object _gate = new();
    private SidecarCircuitState _circuitState;
    private int _consecutiveFailures;
    private DateTimeOffset _openUntil;

    /// <summary>Initializes a new sidecar wrapping <paramref name="primary"/>.</summary>
    public Sidecar(TService primary, SidecarOptions options, ILogger<Sidecar<TService>>? logger = null)
    {
        Primary = primary ?? throw new ArgumentNullException(nameof(primary));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<Sidecar<TService>>.Instance;
        _halfOpenGate = new SemaphoreSlim(
            Math.Max(1, options.HalfOpenPermits),
            Math.Max(1, options.HalfOpenPermits));
    }

    /// <summary>Initializes a new sidecar from DI-injected options.</summary>
    public Sidecar(TService primary, IOptions<SidecarOptions> options, ILogger<Sidecar<TService>>? logger = null)
        : this(primary, options.Value, logger) { }

    /// <inheritdoc />
    public TService Primary { get; }

    /// <inheritdoc />
    public SidecarCircuitState CircuitState
    {
        get
        {
            lock (_gate) { RefreshCircuit(); return _circuitState; }
        }
    }

    /// <inheritdoc />
    public ISidecarMetrics Metrics => _metrics;

    /// <inheritdoc />
    public SidecarOptions Options => _options;

    // ── Public execute overloads ──────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<TResult> ExecuteAsync<TResult>(
        Func<TService, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        _metrics.RecordCall();
        EnsureCircuitAllowsCall();

        var halfOpenAcquired = await TryAcquireHalfOpenPermitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ExecuteWithRetryAsync(operation, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (halfOpenAcquired)
                _halfOpenGate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask ExecuteAsync(
        Func<TService, CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await ExecuteAsync<bool>(async (svc, ct) =>
        {
            await operation(svc, ct).ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    // ── Retry loop ────────────────────────────────────────────────────────────

    private async ValueTask<TResult> ExecuteWithRetryAsync<TResult>(
        Func<TService, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, _options.MaxAttempts);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await ExecuteWithTimeoutAsync(operation, cancellationToken).ConfigureAwait(false);
                OnSuccess();
                _metrics.RecordSuccess();
                return result;
            }
            catch (TimeoutException ex)
            {
                _metrics.RecordTimeout();
                lastException = ex;
                OnFailure(ex);

                if (attempt < maxAttempts)
                {
                    _metrics.RecordRetry();
                    await DelayAsync(attempt, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (SidecarCircuitOpenException)
            {
                // Circuit opened mid-retry — stop immediately and propagate.
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                OnFailure(ex);

                if (attempt < maxAttempts)
                {
                    _metrics.RecordRetry();
                    _logger.LogWarning(ex,
                        "Sidecar retry attempt {Attempt}/{Max} for {Service}. Next delay: {Delay}.",
                        attempt, maxAttempts, typeof(TService).Name, ComputeDelay(attempt));

                    await DelayAsync(attempt, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        _metrics.RecordFailure();
        ExceptionDispatchInfo.Capture(lastException!).Throw();
        throw new UnreachableException(); // satisfies the compiler
    }

    // ── Timeout wrapper ───────────────────────────────────────────────────────

    private async ValueTask<TResult> ExecuteWithTimeoutAsync<TResult>(
        Func<TService, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken)
    {
        if (_options.ExecutionTimeout is null)
            return await operation(Primary, cancellationToken).ConfigureAwait(false);

        var task = operation(Primary, cancellationToken).AsTask();
        return await task.WaitAsync(_options.ExecutionTimeout.Value, cancellationToken).ConfigureAwait(false);
    }

    // ── Circuit breaker helpers ───────────────────────────────────────────────

    private void EnsureCircuitAllowsCall()
    {
        lock (_gate)
        {
            RefreshCircuit();
            if (_circuitState == SidecarCircuitState.Open)
            {
                _metrics.RecordRejectedByCircuit();
                throw new SidecarCircuitOpenException(
                    $"The sidecar circuit for {typeof(TService).Name} is open until {_openUntil:O}.");
            }
        }
    }

    private async ValueTask<bool> TryAcquireHalfOpenPermitAsync(CancellationToken cancellationToken)
    {
        bool isHalfOpen;
        lock (_gate) { isHalfOpen = _circuitState == SidecarCircuitState.HalfOpen; }

        if (!isHalfOpen)
            return false;

        var acquired = await _halfOpenGate.WaitAsync(0, cancellationToken).ConfigureAwait(false);
        if (!acquired)
        {
            _metrics.RecordRejectedByCircuit();
            throw new SidecarCircuitOpenException(
                $"The sidecar circuit for {typeof(TService).Name} is half-open with no available trial permits.");
        }

        return true;
    }

    private void RefreshCircuit()
    {
        if (_circuitState != SidecarCircuitState.Open)
            return;

        if (_options.TimeProvider.GetUtcNow() >= _openUntil)
        {
            _circuitState = SidecarCircuitState.HalfOpen;
            _consecutiveFailures = 0;
            _logger.LogInformation(
                "Sidecar circuit for {Service} transitioned to HalfOpen.", typeof(TService).Name);
        }
    }

    private void OnSuccess()
    {
        lock (_gate)
        {
            if (_circuitState == SidecarCircuitState.HalfOpen)
            {
                _metrics.RecordCircuitReset();
                _logger.LogInformation(
                    "Sidecar circuit for {Service} closed after successful probe.", typeof(TService).Name);
            }

            _circuitState = SidecarCircuitState.Closed;
            _consecutiveFailures = 0;
        }
    }

    private void OnFailure(Exception exception)
    {
        lock (_gate)
        {
            _consecutiveFailures++;

            var shouldTrip =
                _consecutiveFailures >= _options.FailureThreshold ||
                _circuitState == SidecarCircuitState.HalfOpen;

            if (!shouldTrip)
                return;

            _circuitState = SidecarCircuitState.Open;
            _openUntil = _options.TimeProvider.GetUtcNow().Add(_options.CircuitOpenDuration);
            _consecutiveFailures = 0;
            _metrics.RecordCircuitTrip();

            _logger.LogWarning(exception,
                "Sidecar circuit for {Service} tripped to Open until {Until:O}.",
                typeof(TService).Name, _openUntil);
        }
    }

    // ── Back-off delay ────────────────────────────────────────────────────────

    private ValueTask DelayAsync(int attempt, CancellationToken cancellationToken)
    {
        var delay = ComputeDelay(attempt);
        return delay <= TimeSpan.Zero
            ? ValueTask.CompletedTask
            : new ValueTask(Task.Delay(delay, _options.TimeProvider, cancellationToken));
    }

    private TimeSpan ComputeDelay(int attempt)
    {
        var raw = _options.RetryStrategy switch
        {
            SidecarRetryStrategy.Linear =>
                TimeSpan.FromTicks(_options.RetryBaseDelay.Ticks * attempt),

            SidecarRetryStrategy.Exponential =>
                TimeSpan.FromTicks(_options.RetryBaseDelay.Ticks * (long)Math.Pow(2, attempt - 1)),

            _ /* Fixed */ =>
                _options.RetryBaseDelay
        };

        // Cap to maximum delay
        var capped = raw > _options.RetryMaxDelay ? _options.RetryMaxDelay : raw;

        if (!_options.UseJitter || capped <= TimeSpan.Zero)
            return capped;

        // Decorrelated jitter: uniform [0, 2 * capped] centred on the computed value
        var jitterTicks = (long)(capped.Ticks * Random.Shared.NextDouble());
        return TimeSpan.FromTicks(Math.Max(0, capped.Ticks + jitterTicks - capped.Ticks / 2));
    }
}
