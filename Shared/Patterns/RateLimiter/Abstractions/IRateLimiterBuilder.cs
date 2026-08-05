namespace Usm.Shared.Patterns.RateLimiter.Abstractions;

/// <summary>
/// Fluent builder for configuring a rate limiter.
/// </summary>
/// <typeparam name="TContext">The calling context type.</typeparam>
public interface IRateLimiterBuilder<TContext>
{
    /// <summary>Sets the algorithm.</summary>
    IRateLimiterBuilder<TContext> WithAlgorithm(RateLimiterAlgorithm algorithm);

    /// <summary>Sets the permit limit.</summary>
    IRateLimiterBuilder<TContext> WithPermitLimit(int permitLimit);

    /// <summary>Sets the time window used by the limiter.</summary>
    IRateLimiterBuilder<TContext> WithWindow(TimeSpan window);

    /// <summary>Sets the number of segments used by sliding windows.</summary>
    IRateLimiterBuilder<TContext> WithSegments(int segments);

    /// <summary>Sets the queue limit used by the leaky bucket limiter.</summary>
    IRateLimiterBuilder<TContext> WithQueueLimit(int queueLimit);

    /// <summary>Sets the time provider.</summary>
    IRateLimiterBuilder<TContext> WithTimeProvider(TimeProvider timeProvider);

    /// <summary>Sets the logger.</summary>
    IRateLimiterBuilder<TContext> WithLogger(Microsoft.Extensions.Logging.ILogger<IRateLimiter<TContext>> logger);

    /// <summary>Builds the rate limiter.</summary>
    IRateLimiter<TContext> Build();
}
