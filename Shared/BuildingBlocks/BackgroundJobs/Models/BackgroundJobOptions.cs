namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Models;

/// <summary>
/// Configuration options for the background job processor.
/// Bind to configuration section <c>"BackgroundJobs"</c> or set programmatically.
/// </summary>
public sealed class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    /// <summary>How often the processor polls for pending jobs. Default: 5 s.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Maximum number of jobs processed per polling cycle. Default: 1.</summary>
    public int MaxJobsPerCycle { get; set; } = 1;

    /// <summary>Maximum number of retry attempts before marking a job as Failed. Default: 3.</summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>Base delay before the first retry. Subsequent retries use exponential back-off.</summary>
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Multiplier applied to the retry delay on each successive attempt. Default: 2.</summary>
    public double RetryBackoffMultiplier { get; set; } = 2.0;

    /// <summary>Cap on the computed retry delay. Default: 10 min.</summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(10);
}
