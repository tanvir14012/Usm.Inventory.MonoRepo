namespace Usm.Shared.Patterns.CircuitBreaker;

/// <summary>
/// Immutable circuit breaker metrics snapshot.
/// </summary>
public sealed record CircuitBreakerMetricsSnapshot(long Failures, long Trips, long Resets, long Timeouts);
