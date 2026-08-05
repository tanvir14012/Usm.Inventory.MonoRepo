using Usm.Shared.Patterns.CircuitBreaker;

namespace Usm.Shared.Patterns.CircuitBreaker.Abstractions;

/// <summary>
/// Describes a reusable circuit breaker policy for synchronous and asynchronous operations.
/// </summary>
/// <typeparam name="TContext">The operation context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public interface ICircuitBreakerPolicy<TContext, TResult>
{
    /// <summary>Gets the current breaker state.</summary>
    CircuitBreakerState State { get; }

    /// <summary>Gets the configured options.</summary>
    CircuitBreakerOptions Options { get; }

    /// <summary>Gets the metrics collector.</summary>
    ICircuitBreakerMetrics Metrics { get; }

    /// <summary>Executes the operation synchronously.</summary>
    TResult Execute(TContext context, Func<TContext, TResult> operation);

    /// <summary>Executes the operation asynchronously.</summary>
    ValueTask<TResult> ExecuteAsync(
        TContext context,
        Func<TContext, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default);
}
