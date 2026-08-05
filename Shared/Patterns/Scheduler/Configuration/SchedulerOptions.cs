namespace Usm.Shared.Patterns.Scheduler;

/// <summary>
/// Configuration for the scheduler.
/// </summary>
public sealed class SchedulerOptions
{
    /// <summary>Gets or sets the batch size for running jobs.</summary>
    public int BatchSize { get; set; } = 32;

    /// <summary>Gets or sets the time provider.</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>Gets or sets the retry delay strategy.</summary>
    public Func<int, TimeSpan> RetryDelayStrategy { get; set; } = attempt => TimeSpan.Zero;
}
