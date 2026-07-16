namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Abstractions;

/// <summary>
/// Enqueues background Excel import jobs for asynchronous, persistent execution.
/// </summary>
public interface IJobScheduler
{
    /// <summary>
    /// Persists a new job with an optional typed payload.
    /// Returns the assigned <see cref="Guid"/> job ID, or <see cref="Guid.Empty"/> when
    /// the <paramref name="idempotencyKey"/> matches an existing non-terminal job.
    /// </summary>
    Task<Guid> EnqueueAsync<TJob>(
        object? payload = null,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        where TJob : IExcelJob;

    /// <summary>
    /// Persists a new job with an Excel stream as its payload.
    /// The stream is read fully, Base64-encoded, and stored as JSON.
    /// For very large files consider saving to blob storage first and passing a reference payload.
    /// </summary>
    Task<Guid> EnqueueAsync<TJob>(
        Stream excelStream,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        where TJob : IExcelJob;

    /// <summary>Marks a pending or running job as Cancelled.</summary>
    Task CancelAsync(Guid jobId, CancellationToken cancellationToken = default);
}
