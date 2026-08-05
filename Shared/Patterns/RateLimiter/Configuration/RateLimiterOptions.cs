namespace Usm.Shared.Patterns.RateLimiter;

/// <summary>
/// Configuration for rate limiting.
/// </summary>
public sealed class RateLimiterOptions
{
    /// <summary>Gets or sets the algorithm used by the limiter.</summary>
    public RateLimiterAlgorithm Algorithm { get; set; } = RateLimiterAlgorithm.TokenBucket;

    /// <summary>Gets or sets the maximum permits allowed in the window.</summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>Gets or sets the window used by fixed, token, sliding, and leaky algorithms.</summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Gets or sets the number of segments for sliding windows.</summary>
    public int Segments { get; set; } = 4;

    /// <summary>Gets or sets the queue limit for leaky bucket behavior.</summary>
    public int QueueLimit { get; set; } = 100;

    /// <summary>Gets or sets the time provider.</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;
}
