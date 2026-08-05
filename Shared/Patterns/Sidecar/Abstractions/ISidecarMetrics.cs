using Usm.Shared.Patterns.Sidecar.Models;

namespace Usm.Shared.Patterns.Sidecar.Abstractions;

/// <summary>
/// Live metrics exposed by a sidecar instance.
/// </summary>
public interface ISidecarMetrics
{
    /// <summary>Total calls dispatched (all outcomes).</summary>
    long TotalCalls { get; }

    /// <summary>Calls that completed successfully.</summary>
    long Successes { get; }

    /// <summary>Calls that failed after exhausting retry attempts.</summary>
    long Failures { get; }

    /// <summary>Individual retry attempts made.</summary>
    long Retries { get; }

    /// <summary>Calls aborted due to execution timeout.</summary>
    long Timeouts { get; }

    /// <summary>Times the circuit breaker tripped to open.</summary>
    long CircuitTrips { get; }

    /// <summary>Times the circuit breaker closed after a successful probe.</summary>
    long CircuitResets { get; }

    /// <summary>Calls rejected while the circuit was open.</summary>
    long RejectedByCircuit { get; }

    /// <summary>Captures a point-in-time snapshot of all counters.</summary>
    SidecarMetricsSnapshot Snapshot(SidecarCircuitState currentState);
}
