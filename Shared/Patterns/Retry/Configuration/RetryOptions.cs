namespace Usm.Shared.Patterns.Retry;

/// <summary>
/// Supported retry delay strategies.
/// </summary>
public enum RetryStrategy
{
    /// <summary>Use the same delay for every attempt.</summary>
    Fixed = 0,

    /// <summary>Multiply the base delay by the attempt number.</summary>
    Linear = 1,

    /// <summary>Multiply the base delay exponentially.</summary>
    Exponential = 2,

    /// <summary>Use a custom strategy delegate.</summary>
    Custom = 3
}

/// <summary>
/// Configuration for retry policies.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>Gets or sets the maximum number of attempts.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Gets or sets the base delay.</summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(50);

    /// <summary>Gets or sets the retry strategy.</summary>
    public RetryStrategy Strategy { get; set; } = RetryStrategy.Fixed;

    /// <summary>Gets or sets a value indicating whether jitter is enabled.</summary>
    public bool UseJitter { get; set; }

    /// <summary>Gets or sets the jitter ratio.</summary>
    public double JitterRatio { get; set; } = 0.1d;

    /// <summary>Gets or sets the custom delay strategy.</summary>
    public Func<int, TimeSpan>? CustomDelayStrategy { get; set; }

    /// <summary>Gets or sets the time provider used for delays.</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;
}
