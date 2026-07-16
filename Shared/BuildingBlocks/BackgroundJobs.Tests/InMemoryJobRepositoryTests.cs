using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Abstractions;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Internal;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Models;
using Xunit;

namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Tests;

public sealed class InMemoryJobRepositoryTests
{
    private static InMemoryJobRepository Repo() => new();

    [Fact]
    public async Task SaveAndGetById_RoundTrips()
    {
        var repo   = Repo();
        var record = new JobRecord { JobType = "MyJob", Priority = 1 };

        await repo.SaveAsync(record);
        var found = await repo.GetByIdAsync(record.JobId);

        Assert.NotNull(found);
        Assert.Equal(record.JobId, found!.JobId);
    }

    [Fact]
    public async Task GetPendingJobs_ReturnsOnlyPending()
    {
        var repo = Repo();
        await repo.SaveAsync(new JobRecord { Priority = 1, Status = JobStatus.Pending });
        await repo.SaveAsync(new JobRecord { Priority = 2, Status = JobStatus.Completed });
        await repo.SaveAsync(new JobRecord { Priority = 3, Status = JobStatus.Failed });

        var pending = await repo.GetPendingJobsAsync();
        Assert.Single(pending);
        Assert.Equal(JobStatus.Pending, pending[0].Status);
    }

    [Fact]
    public async Task GetPendingJobs_IsOrderedByPriorityThenCreatedAt()
    {
        var repo = Repo();
        var r3 = new JobRecord { Priority = 3, CreatedAt = DateTimeOffset.UtcNow };
        var r1 = new JobRecord { Priority = 1, CreatedAt = DateTimeOffset.UtcNow.AddSeconds(-2) };
        var r2 = new JobRecord { Priority = 1, CreatedAt = DateTimeOffset.UtcNow.AddSeconds(-1) };

        await repo.SaveAsync(r3);
        await repo.SaveAsync(r1);
        await repo.SaveAsync(r2);

        var jobs = await repo.GetPendingJobsAsync();
        Assert.Equal(r1.JobId, jobs[0].JobId);
        Assert.Equal(r2.JobId, jobs[1].JobId);
        Assert.Equal(r3.JobId, jobs[2].JobId);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrueForNonTerminalJob()
    {
        var repo   = Repo();
        var record = new JobRecord { IdempotencyKey = "k1", Status = JobStatus.Pending };
        await repo.SaveAsync(record);

        Assert.True(await repo.ExistsAsync("k1"));
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalseForFailedJob()
    {
        var repo   = Repo();
        var record = new JobRecord { IdempotencyKey = "k2", Status = JobStatus.Failed };
        await repo.SaveAsync(record);

        Assert.False(await repo.ExistsAsync("k2"));
    }

    [Fact]
    public async Task RecoverInterruptedJobs_ResetsRunningToPending()
    {
        var repo   = Repo();
        var record = new JobRecord { Status = JobStatus.Running };
        await repo.SaveAsync(record);

        await repo.RecoverInterruptedJobsAsync();

        var updated = await repo.GetByIdAsync(record.JobId);
        Assert.Equal(JobStatus.Pending, updated!.Status);
        Assert.Null(updated.StartedAt);
    }
}
