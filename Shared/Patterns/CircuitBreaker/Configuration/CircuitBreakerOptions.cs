namespace Usm.Shared.Patterns.CircuitBreaker;

/// <summary>
/// Supported circuit breaker states.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>The breaker allows calls.</summary>
    Closed = 0,

    /// <summary>The breaker rejects calls.</summary>
    Open = 1,

    /// <summary>The breaker allows limited trial calls.</summary>
    HalfOpen = 2
}

/// <summary>
/// Configuration for circuit breaker policies.
/// </summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>Gets or sets the number of failures before opening the breaker.</summary>
    public int FailureThreshold { get; set; } = 3;

    /// <summary>Gets or sets the duration the breaker stays open.</summary>
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the maximum time allowed for a single operation.</summary>
    public TimeSpan? ExecutionTimeout { get; set; }

    /// <summary>Gets or sets the number of half-open trial calls permitted.</summary>
    public int HalfOpenPermits { get; set; } = 1;

    /// <summary>Gets or sets the time provider used by the policy.</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;
}
