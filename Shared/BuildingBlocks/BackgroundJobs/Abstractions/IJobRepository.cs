using Usm.Shared.BuildingBlocks.BackgroundJobs.Models;

namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Abstractions;

/// <summary>
/// Persistent job queue abstraction (Outbox pattern).
/// The default implementation (<c>InMemoryJobRepository</c>) is suitable for development
/// and testing only. Register a durable EF Core / database-backed implementation for
/// production by calling <c>services.AddJobRepository&lt;MyRepository&gt;()</c>
/// after <c>AddBackgroundJobs()</c>.
/// </summary>
public interface IJobRepository
{
    Task SaveAsync(JobRecord record, CancellationToken cancellationToken = default);

    /// <summary>Returns pending jobs ordered by Priority asc, then CreatedAt asc.</summary>
    Task<IReadOnlyList<JobRecord>> GetPendingJobsAsync(
        int maxCount = 50,
        CancellationToken cancellationToken = default);

    Task<JobRecord?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task UpdateAsync(JobRecord record, CancellationToken cancellationToken = default);

    /// <summary>Returns true when a non-terminal job with <paramref name="idempotencyKey"/> already exists.</summary>
    Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets any jobs that were in <see cref="JobStatus.Running"/> state (interrupted by a
    /// process restart) back to <see cref="JobStatus.Pending"/> so they are retried.
    /// </summary>
    Task RecoverInterruptedJobsAsync(CancellationToken cancellationToken = default);
}
