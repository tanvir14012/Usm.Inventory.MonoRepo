namespace Usm.Shared.Patterns.RateLimiter.Abstractions;

/// <summary>
/// Performs asynchronous rate-limited acquisition for a context.
/// </summary>
/// <typeparam name="TContext">The calling context type.</typeparam>
public interface IRateLimiter<TContext>
{
    /// <summary>Attempts to acquire the requested number of permits.</summary>
    ValueTask<RateLimitLease> AcquireAsync(TContext context, int permits = 1, CancellationToken cancellationToken = default);
}
