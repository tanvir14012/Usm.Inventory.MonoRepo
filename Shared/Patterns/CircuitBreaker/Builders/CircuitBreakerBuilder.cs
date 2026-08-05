using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.CircuitBreaker;
using Usm.Shared.Patterns.CircuitBreaker.Abstractions;
using Usm.Shared.Patterns.CircuitBreaker.Extensions;

namespace Usm.Shared.Patterns.CircuitBreaker.Builders;

/// <summary>
/// Fluent builder for a reusable circuit breaker policy.
/// </summary>
/// <typeparam name="TContext">The operation context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public sealed class CircuitBreakerBuilder<TContext, TResult> : ICircuitBreakerBuilder<TContext, TResult>
{
    private readonly CircuitBreakerOptions _options = new();

    /// <inheritdoc />
    public ICircuitBreakerBuilder<TContext, TResult> WithFailureThreshold(int failureThreshold)
    {
        _options.FailureThreshold = failureThreshold > 0 ? failureThreshold : throw new ArgumentOutOfRangeException(nameof(failureThreshold));
        return this;
    }

    /// <inheritdoc />
    public ICircuitBreakerBuilder<TContext, TResult> WithOpenDuration(TimeSpan openDuration)
    {
        _options.OpenDuration = openDuration > TimeSpan.Zero ? openDuration : throw new ArgumentOutOfRangeException(nameof(openDuration));
        return this;
    }

    /// <inheritdoc />
    public ICircuitBreakerBuilder<TContext, TResult> WithExecutionTimeout(TimeSpan? executionTimeout)
    {
        if (executionTimeout is { } timeout && timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(executionTimeout));

        _options.ExecutionTimeout = executionTimeout;
        return this;
    }

    /// <inheritdoc />
    public ICircuitBreakerBuilder<TContext, TResult> WithHalfOpenPermits(int permits)
    {
        _options.HalfOpenPermits = permits > 0 ? permits : throw new ArgumentOutOfRangeException(nameof(permits));
        return this;
    }

    /// <inheritdoc />
    public ICircuitBreakerBuilder<TContext, TResult> WithTimeProvider(TimeProvider timeProvider)
    {
        _options.TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        return this;
    }

    /// <inheritdoc />
    public ICircuitBreakerPolicy<TContext, TResult> Build()
        => new CircuitBreakerPolicy<TContext, TResult>(Options.Create(_options));
}
