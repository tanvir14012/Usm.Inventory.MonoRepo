using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.Retry;
using Usm.Shared.Patterns.Retry.Abstractions;
using Usm.Shared.Patterns.Retry.Extensions;

namespace Usm.Shared.Patterns.Retry.Builders;

/// <summary>
/// Fluent builder for a reusable retry policy.
/// </summary>
/// <typeparam name="TContext">The operation context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public sealed class RetryBuilder<TContext, TResult> : IRetryBuilder<TContext, TResult>
{
    private readonly RetryOptions _options = new();

    /// <inheritdoc />
    public IRetryBuilder<TContext, TResult> WithMaxAttempts(int maxAttempts)
    {
        _options.MaxAttempts = maxAttempts > 0 ? maxAttempts : throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        return this;
    }

    /// <inheritdoc />
    public IRetryBuilder<TContext, TResult> WithDelay(TimeSpan delay)
    {
        _options.Delay = delay >= TimeSpan.Zero ? delay : throw new ArgumentOutOfRangeException(nameof(delay));
        return this;
    }

    /// <inheritdoc />
    public IRetryBuilder<TContext, TResult> WithStrategy(RetryStrategy strategy)
    {
        _options.Strategy = strategy;
        return this;
    }

    /// <inheritdoc />
    public IRetryBuilder<TContext, TResult> WithJitter(bool enabled)
    {
        _options.UseJitter = enabled;
        return this;
    }

    /// <inheritdoc />
    public IRetryBuilder<TContext, TResult> WithCustomDelayStrategy(Func<int, TimeSpan> strategy)
    {
        _options.CustomDelayStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _options.Strategy = RetryStrategy.Custom;
        return this;
    }

    /// <inheritdoc />
    public IRetryBuilder<TContext, TResult> WithTimeProvider(TimeProvider timeProvider)
    {
        _options.TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        return this;
    }

    /// <inheritdoc />
    public IRetryPolicy<TContext, TResult> Build()
        => new RetryPolicy<TContext, TResult>(Options.Create(_options));
}
