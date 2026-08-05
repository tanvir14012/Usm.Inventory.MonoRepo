namespace Usm.Shared.Patterns.Sidecar;

/// <summary>
/// Resilience strategy applied by the sidecar retry pipeline.
/// </summary>
public enum SidecarRetryStrategy
{
    /// <summary>Fixed delay between attempts.</summary>
    Fixed = 0,

    /// <summary>Linearly increasing delay: baseDelay * attempt.</summary>
    Linear = 1,

    /// <summary>Exponential back-off: baseDelay * 2^(attempt-1) with optional jitter.</summary>
    Exponential = 2
}

/// <summary>
/// Configuration for a sidecar instance.
/// </summary>
public sealed class SidecarOptions
{
    // ── Retry ─────────────────────────────────────────────────────────────────

    /// <summary>Maximum number of call attempts (1 = no retry).</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Base delay between attempts.</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>Maximum delay cap for any single back-off interval.</summary>
    public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Delay strategy used when computing the back-off interval.</summary>
    public SidecarRetryStrategy RetryStrategy { get; set; } = SidecarRetryStrategy.Exponential;

    /// <summary>Adds decorrelated jitter to the computed delay when true.</summary>
    public bool UseJitter { get; set; } = true;

    // ── Circuit Breaker ───────────────────────────────────────────────────────

    /// <summary>Number of consecutive failures before opening the circuit.</summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>Duration the circuit stays open before transitioning to half-open.</summary>
    public TimeSpan CircuitOpenDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Number of trial calls permitted in the half-open state.</summary>
    public int HalfOpenPermits { get; set; } = 1;

    // ── Timeout ───────────────────────────────────────────────────────────────

    /// <summary>Per-call execution timeout. Null disables the timeout.</summary>
    public TimeSpan? ExecutionTimeout { get; set; }

    // ── Health ────────────────────────────────────────────────────────────────

    /// <summary>Name used when registering the health check for this sidecar.</summary>
    public string HealthCheckName { get; set; } = "sidecar";

    /// <summary>Consecutive successes required to mark the sidecar healthy after a failure streak.</summary>
    public int HealthRecoverySuccessCount { get; set; } = 3;

    /// <summary>Allows injecting a fake clock for tests.</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;
}
