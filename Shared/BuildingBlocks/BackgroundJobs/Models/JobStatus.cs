namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Models;

public enum JobStatus
{
    Pending   = 0,
    Running   = 1,
    Completed = 2,
    Failed    = 3,
    Cancelled = 4
}
