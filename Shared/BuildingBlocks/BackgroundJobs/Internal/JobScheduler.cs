using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Abstractions;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Models;

namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Internal;

internal sealed class JobScheduler(
    IJobRepository repository,
    IServiceProvider serviceProvider,
    ILogger<JobScheduler> logger) : IJobScheduler
{
    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    public async Task<Guid> EnqueueAsync<TJob>(
        object? payload = null,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        where TJob : IExcelJob
    {
        // Idempotency guard
        if (idempotencyKey is not null
            && await repository.ExistsAsync(idempotencyKey, cancellationToken))
        {
            logger.LogWarning(
                "Duplicate job suppressed. IdempotencyKey={Key} Type={Type}",
                idempotencyKey, typeof(TJob).Name);
            return Guid.Empty;
        }

        // Resolve job briefly to read its Priority declaration
        int priority;
        using (var scope = serviceProvider.CreateScope())
        {
            var job  = scope.ServiceProvider.GetRequiredService<TJob>();
            priority = job.Priority;
        }

        var record = new JobRecord
        {
            JobType           = typeof(TJob).AssemblyQualifiedName!,
            SerializedPayload = payload is not null
                ? JsonSerializer.Serialize(payload, JsonOpts)
                : null,
            Priority       = priority,
            IdempotencyKey = idempotencyKey
        };

        await repository.SaveAsync(record, cancellationToken);

        logger.LogInformation(
            "Job enqueued. JobId={JobId} Type={Type} Priority={Priority}",
            record.JobId, typeof(TJob).Name, priority);

        return record.JobId;
    }

    public async Task<Guid> EnqueueAsync<TJob>(
        Stream excelStream,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        where TJob : IExcelJob
    {
        using var ms = new MemoryStream();
        await excelStream.CopyToAsync(ms, cancellationToken);
        var payload = new StreamPayload(Convert.ToBase64String(ms.ToArray()));
        return await EnqueueAsync<TJob>(payload, idempotencyKey, cancellationToken);
    }

    public async Task CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var record = await repository.GetByIdAsync(jobId, cancellationToken);
        if (record is null)
        {
            logger.LogWarning("Cancel: job not found. JobId={JobId}", jobId);
            return;
        }

        if (record.Status is JobStatus.Completed or JobStatus.Cancelled or JobStatus.Failed)
        {
            logger.LogWarning(
                "Cannot cancel job in terminal state. JobId={JobId} Status={Status}",
                jobId, record.Status);
            return;
        }

        record.Status = JobStatus.Cancelled;
        await repository.UpdateAsync(record, cancellationToken);

        logger.LogInformation("Job cancelled. JobId={JobId}", jobId);
    }
}
