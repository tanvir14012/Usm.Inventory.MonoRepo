namespace Usm.Shared.Patterns.Workflow.Configuration;

/// <summary>
/// Supported retry delay strategies.
/// </summary>
public enum RetryDelayStrategy
{
    /// <summary>Use the same delay on each attempt.</summary>
    Fixed = 0,

    /// <summary>Increase delay linearly with the attempt number.</summary>
    Linear = 1,

    /// <summary>Increase delay exponentially with the attempt number.</summary>
    Exponential = 2
}

/// <summary>
/// Configuration for retrying workflow steps.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>Gets or sets the maximum number of attempts.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Gets or sets the base delay between attempts.</summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(50);

    /// <summary>Gets or sets the retry strategy.</summary>
    public RetryDelayStrategy Strategy { get; set; } = RetryDelayStrategy.Fixed;

    /// <summary>Gets or sets a value indicating whether small jitter should be applied to the delay.</summary>
    public bool UseJitter { get; set; }
}
