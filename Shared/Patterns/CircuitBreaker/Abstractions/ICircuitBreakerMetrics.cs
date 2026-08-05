using Usm.Shared.Patterns.CircuitBreaker;

namespace Usm.Shared.Patterns.CircuitBreaker.Abstractions;

/// <summary>
/// Tracks circuit breaker transitions and failures.
/// </summary>
public interface ICircuitBreakerMetrics
{
    /// <summary>Gets the number of failures observed.</summary>
    long Failures { get; }

    /// <summary>Gets the number of times the breaker opened.</summary>
    long Trips { get; }

    /// <summary>Gets the number of successful closes from half-open.</summary>
    long Resets { get; }

    /// <summary>Gets the number of timeout failures.</summary>
    long Timeouts { get; }

    /// <summary>Gets a snapshot of the current metrics.</summary>
    CircuitBreakerMetricsSnapshot Snapshot();
}
