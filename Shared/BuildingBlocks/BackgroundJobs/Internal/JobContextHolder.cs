using Usm.Shared.BuildingBlocks.BackgroundJobs.Models;

namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Internal;

/// <summary>
/// Scoped mutable container that the background service populates before executing a job.
/// Keeps the current <see cref="JobRecord"/> accessible via <see cref="ICurrentJobContext"/>
/// without requiring the job itself to know about the executor internals.
/// </summary>
internal sealed class JobContextHolder
{
    private JobRecord? _record;

    internal void Set(JobRecord record) => _record = record;

    internal JobRecord Get()
        => _record ?? throw new InvalidOperationException("No active job context in this scope.");
}
