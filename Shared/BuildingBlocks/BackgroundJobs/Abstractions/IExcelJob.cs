namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Abstractions;

/// <summary>
/// Marker interface for background Excel import jobs.
/// Implementations are discovered automatically by Scrutor assembly scanning — no
/// manual registration is required beyond calling <c>services.AddBackgroundJobs()</c>.
/// </summary>
public interface IExcelJob
{
    /// <summary>
    /// Execution priority. Lower values run first (1 before 2 before 3 …).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Execute the job. Inject <see cref="ICurrentJobContext"/> to access the serialized
    /// payload that was stored when the job was enqueued.
    /// </summary>
    Task ExecuteAsync(CancellationToken cancellationToken);
}
