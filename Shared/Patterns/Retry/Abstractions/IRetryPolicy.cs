using Usm.Shared.Patterns.Retry;

namespace Usm.Shared.Patterns.Retry.Abstractions;

/// <summary>
/// Describes a reusable retry policy for synchronous and asynchronous operations.
/// </summary>
/// <typeparam name="TContext">The operation context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public interface IRetryPolicy<TContext, TResult>
{
    /// <summary>Gets the configured retry options.</summary>
    RetryOptions Options { get; }

    /// <summary>Executes the operation synchronously with retry behavior.</summary>
    TResult Execute(TContext context, Func<TContext, TResult> operation);

    /// <summary>Executes the operation asynchronously with retry behavior.</summary>
    ValueTask<TResult> ExecuteAsync(
        TContext context,
        Func<TContext, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default);
}
