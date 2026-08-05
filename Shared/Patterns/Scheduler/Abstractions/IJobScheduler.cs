namespace Usm.Shared.Patterns.Scheduler.Abstractions;

/// <summary>
/// Schedules and runs jobs.
/// </summary>
/// <typeparam name="TJob">The job payload type.</typeparam>
public interface IJobScheduler<TJob>
{
    /// <summary>Gets the number of queued jobs.</summary>
    int PendingCount { get; }

    /// <summary>Schedules a job using the provided schedule.</summary>
    ValueTask ScheduleAsync(TJob job, ISchedule schedule, int priority = 0, int maxAttempts = 1, CancellationToken cancellationToken = default);

    /// <summary>Schedules a one-shot delayed job.</summary>
    ValueTask ScheduleDelayedAsync(TJob job, TimeSpan delay, int priority = 0, int maxAttempts = 1, CancellationToken cancellationToken = default);

    /// <summary>Schedules a recurring interval job.</summary>
    ValueTask ScheduleRecurringAsync(TJob job, TimeSpan interval, int priority = 0, int maxAttempts = 1, CancellationToken cancellationToken = default);

    /// <summary>Schedules a job using a cron expression.</summary>
    ValueTask ScheduleCronAsync(TJob job, CronSchedule schedule, int priority = 0, int maxAttempts = 1, CancellationToken cancellationToken = default);

    /// <summary>Runs all due jobs.</summary>
    ValueTask<int> RunDueAsync(CancellationToken cancellationToken = default);
}
