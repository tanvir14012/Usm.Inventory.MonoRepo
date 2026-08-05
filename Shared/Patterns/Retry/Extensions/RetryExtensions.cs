using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.Retry;
using Usm.Shared.Patterns.Retry.Abstractions;
using Usm.Shared.Patterns.Retry.Builders;

namespace Usm.Shared.Patterns.Retry.Extensions;

/// <summary>
/// Common extension methods for retry policy creation and DI registration.
/// </summary>
public static class RetryExtensions
{
    /// <summary>Registers the retry framework with dependency injection.</summary>
    public static IServiceCollection AddRetryFramework(this IServiceCollection services)
    {
        services.AddOptions<RetryOptions>();
        services.TryAddTransient(typeof(RetryBuilder<,>), typeof(RetryBuilder<,>));
        services.TryAddSingleton(typeof(IRetryPolicy<,>), typeof(RetryPolicy<,>));
        return services;
    }
}

/// <summary>
/// Default reusable retry policy.
/// </summary>
/// <typeparam name="TContext">The operation context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public sealed class RetryPolicy<TContext, TResult> : IRetryPolicy<TContext, TResult>
{
    private readonly RetryOptions _options;
    private readonly ILogger<RetryPolicy<TContext, TResult>> _logger;

    /// <summary>Initializes a new retry policy.</summary>
    public RetryPolicy(IOptions<RetryOptions> options, ILogger<RetryPolicy<TContext, TResult>>? logger = null)
    {
        _options = options.Value;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RetryPolicy<TContext, TResult>>.Instance;
    }

    /// <inheritdoc />
    public RetryOptions Options => _options;

    /// <inheritdoc />
    public TResult Execute(TContext context, Func<TContext, TResult> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var attempts = Math.Max(1, _options.MaxAttempts);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                return operation(context);
            }
            catch (Exception ex) when (attempt < attempts)
            {
                lastException = ex;
                var delay = ComputeDelay(attempt);
                if (delay > TimeSpan.Zero)
                    Task.Delay(delay, _options.TimeProvider, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        throw lastException ?? new InvalidOperationException("Retry operation failed.");
    }

    /// <inheritdoc />
    public async ValueTask<TResult> ExecuteAsync(
        TContext context,
        Func<TContext, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var attempts = Math.Max(1, _options.MaxAttempts);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await operation(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < attempts)
            {
                lastException = ex;
                var delay = ComputeDelay(attempt);
                if (delay > TimeSpan.Zero)
                {
                    _logger.LogDebug(ex, "Retry attempt {Attempt} failed. Delaying {Delay}.", attempt, delay);
                    await Task.Delay(delay, _options.TimeProvider, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        throw lastException ?? new InvalidOperationException("Retry operation failed.");
    }

    private TimeSpan ComputeDelay(int attempt)
    {
        var delay = _options.Strategy switch
        {
            RetryStrategy.Linear => TimeSpan.FromTicks(_options.Delay.Ticks * attempt),
            RetryStrategy.Exponential => TimeSpan.FromTicks(_options.Delay.Ticks * (long)Math.Pow(2, attempt - 1)),
            RetryStrategy.Custom => _options.CustomDelayStrategy?.Invoke(attempt) ?? _options.Delay,
            _ => _options.Delay
        };

        if (!_options.UseJitter || delay <= TimeSpan.Zero)
            return delay;

        var jitterTicks = Math.Max(1, (long)(delay.Ticks * _options.JitterRatio));
        var offset = Random.Shared.NextInt64(-jitterTicks, jitterTicks + 1);
        return TimeSpan.FromTicks(Math.Max(0, delay.Ticks + offset));
    }
}
