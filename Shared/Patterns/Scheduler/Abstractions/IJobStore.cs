namespace Usm.Shared.Patterns.Scheduler.Abstractions;

/// <summary>
/// Stores scheduled jobs.
/// </summary>
/// <typeparam name="TJob">The job payload type.</typeparam>
public interface IJobStore<TJob>
{
    /// <summary>Gets the number of pending jobs.</summary>
    int Count { get; }

    /// <summary>Adds a scheduled job.</summary>
    ValueTask EnqueueAsync(ScheduledJob<TJob> job, CancellationToken cancellationToken = default);

    /// <summary>Dequeue jobs that are due.</summary>
    ValueTask<IReadOnlyList<ScheduledJob<TJob>>> DequeueDueAsync(DateTimeOffset utcNow, int maxCount, CancellationToken cancellationToken = default);
}
