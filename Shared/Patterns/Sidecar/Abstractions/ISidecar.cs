using Usm.Shared.Patterns.Sidecar.Models;

namespace Usm.Shared.Patterns.Sidecar.Abstractions;

/// <summary>
/// A sidecar that wraps a primary service, transparently applying resilience
/// (exponential back-off retry, circuit breaker, per-call timeout) and
/// emitting structured telemetry for every call.
/// </summary>
/// <typeparam name="TService">The primary service contract.</typeparam>
public interface ISidecar<TService> where TService : class
{
    /// <summary>Gets the underlying primary service.</summary>
    TService Primary { get; }

    /// <summary>Gets the current circuit breaker state.</summary>
    SidecarCircuitState CircuitState { get; }

    /// <summary>Gets the live metrics for this sidecar.</summary>
    ISidecarMetrics Metrics { get; }

    /// <summary>Gets the options used to configure this sidecar.</summary>
    SidecarOptions Options { get; }

    /// <summary>
    /// Executes <paramref name="operation"/> against the primary service with full
    /// resilience (retry with back-off, circuit breaker, timeout).
    /// </summary>
    /// <typeparam name="TResult">The operation result type.</typeparam>
    /// <param name="operation">The delegate to invoke on the primary.</param>
    /// <param name="cancellationToken">Propagates caller cancellation.</param>
    ValueTask<TResult> ExecuteAsync<TResult>(
        Func<TService, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a fire-and-forget style operation with full resilience.
    /// </summary>
    ValueTask ExecuteAsync(
        Func<TService, CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken = default);
}
