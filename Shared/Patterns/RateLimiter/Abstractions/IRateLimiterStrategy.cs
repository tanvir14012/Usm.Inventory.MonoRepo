namespace Usm.Shared.Patterns.RateLimiter.Abstractions;

/// <summary>
/// Internal strategy for acquisition behavior.
/// </summary>
/// <typeparam name="TContext">The calling context type.</typeparam>
public interface IRateLimiterStrategy<TContext>
{
    /// <summary>Attempts to acquire permits for the given context.</summary>
    RateLimitLease Acquire(TContext context, int permits, DateTimeOffset utcNow);
}
