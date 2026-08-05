using Usm.Shared.Patterns.CircuitBreaker;

namespace Usm.Shared.Patterns.CircuitBreaker.Abstractions;

/// <summary>
/// Fluent builder for a reusable circuit breaker policy.
/// </summary>
/// <typeparam name="TContext">The operation context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public interface ICircuitBreakerBuilder<TContext, TResult>
{
    /// <summary>Sets the failure threshold before the breaker opens.</summary>
    ICircuitBreakerBuilder<TContext, TResult> WithFailureThreshold(int failureThreshold);

    /// <summary>Sets the duration the breaker remains open.</summary>
    ICircuitBreakerBuilder<TContext, TResult> WithOpenDuration(TimeSpan openDuration);

    /// <summary>Sets the execution timeout for the operation.</summary>
    ICircuitBreakerBuilder<TContext, TResult> WithExecutionTimeout(TimeSpan? executionTimeout);

    /// <summary>Sets the number of half-open trial calls allowed.</summary>
    ICircuitBreakerBuilder<TContext, TResult> WithHalfOpenPermits(int permits);

    /// <summary>Sets the time provider used for state transitions.</summary>
    ICircuitBreakerBuilder<TContext, TResult> WithTimeProvider(TimeProvider timeProvider);

    /// <summary>Builds the circuit breaker policy.</summary>
    ICircuitBreakerPolicy<TContext, TResult> Build();
}
