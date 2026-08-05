using Usm.Shared.Patterns.Retry;

namespace Usm.Shared.Patterns.Retry.Abstractions;

/// <summary>
/// Fluent builder for a reusable retry policy.
/// </summary>
/// <typeparam name="TContext">The operation context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public interface IRetryBuilder<TContext, TResult>
{
    /// <summary>Sets the maximum number of attempts.</summary>
    IRetryBuilder<TContext, TResult> WithMaxAttempts(int maxAttempts);

    /// <summary>Sets the base delay between attempts.</summary>
    IRetryBuilder<TContext, TResult> WithDelay(TimeSpan delay);

    /// <summary>Sets the delay strategy.</summary>
    IRetryBuilder<TContext, TResult> WithStrategy(RetryStrategy strategy);

    /// <summary>Enables or disables jitter.</summary>
    IRetryBuilder<TContext, TResult> WithJitter(bool enabled);

    /// <summary>Sets a custom delay strategy.</summary>
    IRetryBuilder<TContext, TResult> WithCustomDelayStrategy(Func<int, TimeSpan> strategy);

    /// <summary>Sets the time provider used for delays.</summary>
    IRetryBuilder<TContext, TResult> WithTimeProvider(TimeProvider timeProvider);

    /// <summary>Builds the retry policy.</summary>
    IRetryPolicy<TContext, TResult> Build();
}
