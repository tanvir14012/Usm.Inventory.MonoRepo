namespace Usm.Shared.Patterns.RateLimiter;

/// <summary>
/// Represents the outcome of a rate-limit acquisition.
/// </summary>
public sealed class RateLimitLease
{
    private RateLimitLease(bool isAcquired, TimeSpan? retryAfter, int remainingPermits)
    {
        IsAcquired = isAcquired;
        RetryAfter = retryAfter;
        RemainingPermits = remainingPermits;
    }

    /// <summary>Gets a value indicating whether the acquisition succeeded.</summary>
    public bool IsAcquired { get; }

    /// <summary>Gets the suggested wait time before retrying.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>Gets the number of permits remaining after acquisition.</summary>
    public int RemainingPermits { get; }

    /// <summary>Creates a successful lease.</summary>
    public static RateLimitLease Acquired(int remainingPermits)
        => new(true, null, remainingPermits);

    /// <summary>Creates a rejected lease.</summary>
    public static RateLimitLease Rejected(TimeSpan retryAfter, int remainingPermits = 0)
        => new(false, retryAfter, remainingPermits);
}

/// <summary>
/// Supported rate-limiting algorithms.
/// </summary>
public enum RateLimiterAlgorithm
{
    /// <summary>Allows bursts up to the permit limit and refills over time.</summary>
    TokenBucket,

    /// <summary>Counts requests in the current fixed window.</summary>
    FixedWindow,

    /// <summary>Counts requests across rolling segments.</summary>
    SlidingWindow,

    /// <summary>Models a constant drain queue.</summary>
    LeakyBucket
}
