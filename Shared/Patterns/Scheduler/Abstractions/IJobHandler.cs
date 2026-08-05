namespace Usm.Shared.Patterns.Scheduler.Abstractions;

/// <summary>
/// Handles a scheduled job payload.
/// </summary>
/// <typeparam name="TJob">The job payload type.</typeparam>
public interface IJobHandler<TJob>
{
    /// <summary>Executes the job.</summary>
    ValueTask HandleAsync(TJob job, CancellationToken cancellationToken = default);
}
