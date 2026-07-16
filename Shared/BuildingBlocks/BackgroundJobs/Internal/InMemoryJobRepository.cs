using System.Collections.Concurrent;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Abstractions;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Models;

namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Internal;

/// <summary>
/// Thread-safe in-memory job repository.
/// Not durable across process restarts — use a database-backed implementation in production.
/// </summary>
public sealed class InMemoryJobRepository : IJobRepository
{
    private readonly ConcurrentDictionary<Guid, JobRecord> _store = new();

    public Task SaveAsync(JobRecord record, CancellationToken cancellationToken = default)
    {
        _store.TryAdd(record.JobId, record);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<JobRecord>> GetPendingJobsAsync(
        int maxCount = 50, CancellationToken cancellationToken = default)
    {
        var jobs = _store.Values
            .Where(j  => j.Status == JobStatus.Pending)
            .OrderBy(j => j.Priority)
            .ThenBy(j  => j.CreatedAt)
            .Take(maxCount)
            .ToList();

        return Task.FromResult<IReadOnlyList<JobRecord>>(jobs);
    }

    public Task<JobRecord?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(jobId, out var record);
        return Task.FromResult(record);
    }

    public Task UpdateAsync(JobRecord record, CancellationToken cancellationToken = default)
    {
        _store[record.JobId] = record;
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var exists = _store.Values.Any(j =>
            j.IdempotencyKey == idempotencyKey &&
            j.Status is not (JobStatus.Failed or JobStatus.Cancelled));

        return Task.FromResult(exists);
    }

    public Task RecoverInterruptedJobsAsync(CancellationToken cancellationToken = default)
    {
        foreach (var record in _store.Values.Where(j => j.Status == JobStatus.Running))
        {
            record.Status    = JobStatus.Pending;
            record.StartedAt = null;
        }
        return Task.CompletedTask;
    }
}
