namespace Usm.Shared.Patterns.Scheduler.Abstractions;

/// <summary>
/// Represents a schedule that can compute the next run time.
/// </summary>
public interface ISchedule
{
    /// <summary>Returns the next run time after the provided time.</summary>
    DateTimeOffset? GetNextRun(DateTimeOffset utcNow);

    /// <summary>Indicates whether the schedule repeats.</summary>
    bool IsRecurring { get; }
}
