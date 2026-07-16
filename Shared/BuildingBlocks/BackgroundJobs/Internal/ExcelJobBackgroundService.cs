using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Abstractions;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Models;

namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Internal;

/// <summary>
/// Long-running <see cref="BackgroundService"/> that polls the job repository and
/// executes <see cref="IExcelJob"/> implementations in priority order.
/// Each job runs inside a dedicated DI scope so scoped services (e.g. DbContext) are
/// properly created and disposed per job execution.
/// </summary>
internal sealed class ExcelJobBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<BackgroundJobOptions> optionsAccessor,
    ILogger<ExcelJobBackgroundService> logger) : BackgroundService
{
    private readonly BackgroundJobOptions _opts = optionsAccessor.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ExcelJobBackgroundService starting.");

        // On startup recover any jobs that were Running when the process was last killed.
        await RecoverAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception in job processing loop.");
            }

            try
            {
                await Task.Delay(_opts.PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("ExcelJobBackgroundService stopped.");
    }

    // ── Recovery ─────────────────────────────────────────────────────────────

    private async Task RecoverAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        await repo.RecoverInterruptedJobsAsync(ct);
        logger.LogInformation("Interrupted job recovery complete.");
    }

    // ── Poll & execute ────────────────────────────────────────────────────────

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        IReadOnlyList<JobRecord> pending;

        using (var scope = serviceProvider.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IJobRepository>();
            pending  = await repo.GetPendingJobsAsync(_opts.MaxJobsPerCycle, ct);
        }

        foreach (var record in pending)
        {
            ct.ThrowIfCancellationRequested();

            if (record.Status == JobStatus.Cancelled)
                continue;

            await ExecuteJobAsync(record, ct);
        }
    }

    private async Task ExecuteJobAsync(JobRecord record, CancellationToken ct)
    {
        using var jobScope = serviceProvider.CreateScope();
        var repo = jobScope.ServiceProvider.GetRequiredService<IJobRepository>();

        // Inject the current job's record into its scope via the holder
        var holder = jobScope.ServiceProvider.GetRequiredService<JobContextHolder>();
        holder.Set(record);

        record.Status    = JobStatus.Running;
        record.StartedAt = DateTimeOffset.UtcNow;
        await repo.UpdateAsync(record, ct);

        logger.LogInformation(
            "Executing job. JobId={JobId} Type={ShortType} Attempt={Attempt}",
            record.JobId, ShortTypeName(record.JobType), record.RetryCount + 1);

        try
        {
            var jobType = Type.GetType(record.JobType)
                ?? throw new InvalidOperationException(
                    $"Cannot resolve job type '{record.JobType}'. " +
                    "Ensure the assembly is referenced and the type is registered in DI.");

            var job = (IExcelJob)jobScope.ServiceProvider.GetRequiredService(jobType);
            await job.ExecuteAsync(ct);

            record.Status       = JobStatus.Completed;
            record.CompletedAt  = DateTimeOffset.UtcNow;
            record.ErrorMessage = null;

            logger.LogInformation("Job completed. JobId={JobId}", record.JobId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Service is shutting down — leave the job as Pending so it restarts cleanly.
            record.Status    = JobStatus.Pending;
            record.StartedAt = null;
            logger.LogWarning("Job interrupted by shutdown. JobId={JobId}", record.JobId);
        }
        catch (Exception ex)
        {
            record.RetryCount++;
            record.ErrorMessage = ex.Message;

            if (record.RetryCount >= _opts.MaxRetryCount)
            {
                record.Status      = JobStatus.Failed;
                record.CompletedAt = DateTimeOffset.UtcNow;
                logger.LogError(ex,
                    "Job failed permanently. JobId={JobId} Attempts={Attempts}",
                    record.JobId, record.RetryCount);
            }
            else
            {
                record.Status = JobStatus.Pending;
                var delay = RetryDelay(record.RetryCount);
                logger.LogWarning(ex,
                    "Job failed; retrying in {Delay}. JobId={JobId} Attempt={Attempt}",
                    delay, record.JobId, record.RetryCount);

                await repo.UpdateAsync(record, ct);
                await Task.Delay(delay, ct);
                return;  // skip the final UpdateAsync below; already saved above
            }
        }
        finally
        {
            // Best-effort final save; swallow if already saved in the retry path
            try { await repo.UpdateAsync(record, CancellationToken.None); }
            catch (Exception ex) { logger.LogError(ex, "Failed to persist final job state. JobId={JobId}", record.JobId); }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private TimeSpan RetryDelay(int retryCount)
    {
        var seconds = _opts.InitialRetryDelay.TotalSeconds
                    * Math.Pow(_opts.RetryBackoffMultiplier, retryCount - 1);
        var delay   = TimeSpan.FromSeconds(seconds);
        return delay > _opts.MaxRetryDelay ? _opts.MaxRetryDelay : delay;
    }

    private static string ShortTypeName(string assemblyQualifiedName)
    {
        var comma = assemblyQualifiedName.IndexOf(',');
        return comma < 0 ? assemblyQualifiedName : assemblyQualifiedName[..comma];
    }
}
