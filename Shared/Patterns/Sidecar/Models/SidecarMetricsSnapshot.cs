using Usm.Shared.Patterns.Sidecar.Models;

namespace Usm.Shared.Patterns.Sidecar.Models;

/// <summary>
/// Immutable point-in-time snapshot of sidecar telemetry.
/// </summary>
/// <param name="TotalCalls">Total calls dispatched through the sidecar (all outcomes).</param>
/// <param name="Successes">Calls that completed without an unhandled exception.</param>
/// <param name="Failures">Calls that threw an exception after exhausting all retry attempts.</param>
/// <param name="Retries">Individual retry attempts made across all calls.</param>
/// <param name="Timeouts">Calls that were aborted due to the execution timeout.</param>
/// <param name="CircuitTrips">Number of times the circuit breaker transitioned to open.</param>
/// <param name="CircuitResets">Number of times the circuit breaker closed after a successful trial.</param>
/// <param name="RejectedByCircuit">Calls rejected because the circuit was open.</param>
/// <param name="CircuitState">Current circuit breaker state at snapshot time.</param>
public sealed record SidecarMetricsSnapshot(
    long TotalCalls,
    long Successes,
    long Failures,
    long Retries,
    long Timeouts,
    long CircuitTrips,
    long CircuitResets,
    long RejectedByCircuit,
    SidecarCircuitState CircuitState);
