namespace Usm.Shared.Patterns.Sidecar.Abstractions;

/// <summary>
/// Fluent builder for configuring and constructing an <see cref="ISidecar{TService}"/>.
/// </summary>
/// <typeparam name="TService">The primary service contract.</typeparam>
public interface ISidecarBuilder<TService> where TService : class
{
    /// <summary>Sets the maximum number of call attempts (1 = no retry).</summary>
    ISidecarBuilder<TService> WithMaxAttempts(int maxAttempts);

    /// <summary>Sets the base delay used by the back-off calculation.</summary>
    ISidecarBuilder<TService> WithRetryBaseDelay(TimeSpan baseDelay);

    /// <summary>Caps the computed back-off at <paramref name="maxDelay"/>.</summary>
    ISidecarBuilder<TService> WithRetryMaxDelay(TimeSpan maxDelay);

    /// <summary>Selects the back-off strategy (Fixed, Linear, Exponential).</summary>
    ISidecarBuilder<TService> WithRetryStrategy(SidecarRetryStrategy strategy);

    /// <summary>Enables or disables decorrelated jitter on computed delays.</summary>
    ISidecarBuilder<TService> WithJitter(bool enabled);

    /// <summary>Sets the failure threshold that trips the circuit breaker.</summary>
    ISidecarBuilder<TService> WithFailureThreshold(int threshold);

    /// <summary>Sets the duration the circuit breaker stays open.</summary>
    ISidecarBuilder<TService> WithCircuitOpenDuration(TimeSpan duration);

    /// <summary>Sets the number of trial calls allowed in half-open state.</summary>
    ISidecarBuilder<TService> WithHalfOpenPermits(int permits);

    /// <summary>Sets per-call execution timeout. Pass null to disable.</summary>
    ISidecarBuilder<TService> WithExecutionTimeout(TimeSpan? timeout);

    /// <summary>Sets a custom <see cref="TimeProvider"/> (useful in tests).</summary>
    ISidecarBuilder<TService> WithTimeProvider(TimeProvider timeProvider);

    /// <summary>Sets the health check name for DI health registration.</summary>
    ISidecarBuilder<TService> WithHealthCheckName(string name);

    /// <summary>
    /// Builds the sidecar wrapping <paramref name="primary"/>.
    /// </summary>
    ISidecar<TService> Build(TService primary);
}
