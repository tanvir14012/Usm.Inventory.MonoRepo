using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Abstractions;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Extensions;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Models;
using Xunit;

namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Tests;

public sealed class JobSchedulerTests
{
    // ── Stub job ──────────────────────────────────────────────────────────────

    private sealed class PriorityTwoJob : IExcelJob
    {
        public int Priority => 2;
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class PriorityOneJob : IExcelJob
    {
        public int Priority => 1;
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed record MyPayload(string FileName, int RowCount);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (IJobScheduler scheduler, IJobRepository repository) BuildScheduler()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register the framework via the public extension (uses InMemoryJobRepository by default)
        services.AddBackgroundJobs();

        // Register stub job types so they can be resolved by the scheduler
        services.AddTransient<PriorityTwoJob>();
        services.AddTransient<PriorityOneJob>();

        var sp    = services.BuildServiceProvider();
        var sched = sp.CreateScope().ServiceProvider.GetRequiredService<IJobScheduler>();
        var repo  = sp.GetRequiredService<IJobRepository>();
        return (sched, repo);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EnqueueAsync_PersistsJobRecord()
    {
        var (sched, repo) = BuildScheduler();

        var id = await sched.EnqueueAsync<PriorityTwoJob>();

        Assert.NotEqual(Guid.Empty, id);
        var record = await repo.GetByIdAsync(id);
        Assert.NotNull(record);
        Assert.Equal(JobStatus.Pending, record!.Status);
        Assert.Equal(2, record.Priority);
    }

    [Fact]
    public async Task EnqueueAsync_SerializesPayload()
    {
        var (sched, repo) = BuildScheduler();
        var payload = new MyPayload("import.xlsx", 200);

        var id     = await sched.EnqueueAsync<PriorityTwoJob>(payload);
        var record = await repo.GetByIdAsync(id);

        Assert.NotNull(record!.SerializedPayload);
        Assert.Contains("import.xlsx", record.SerializedPayload);
    }

    [Fact]
    public async Task EnqueueAsync_IdempotencyKey_DuplicateReturnEmpty()
    {
        var (sched, _) = BuildScheduler();

        var id1 = await sched.EnqueueAsync<PriorityTwoJob>(idempotencyKey: "key-1");
        var id2 = await sched.EnqueueAsync<PriorityTwoJob>(idempotencyKey: "key-1");

        Assert.NotEqual(Guid.Empty, id1);
        Assert.Equal(Guid.Empty, id2);
    }

    [Fact]
    public async Task EnqueueAsync_Stream_StoresBase64Payload()
    {
        var (sched, repo) = BuildScheduler();
        using var ms = new MemoryStream([1, 2, 3, 4, 5]);

        var id     = await sched.EnqueueAsync<PriorityTwoJob>(ms);
        var record = await repo.GetByIdAsync(id);

        Assert.NotNull(record!.SerializedPayload);
        // Payload should contain the StreamPayload type with Base64Data
        Assert.Contains("base64Data", record.SerializedPayload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelAsync_SetsCancelledStatus()
    {
        var (sched, repo) = BuildScheduler();
        var id = await sched.EnqueueAsync<PriorityTwoJob>();

        await sched.CancelAsync(id);

        var record = await repo.GetByIdAsync(id);
        Assert.Equal(JobStatus.Cancelled, record!.Status);
    }

    [Fact]
    public async Task GetPendingJobs_OrdersByPriorityThenCreatedAt()
    {
        var (sched, repo) = BuildScheduler();

        // Enqueue lower priority first
        await sched.EnqueueAsync<PriorityTwoJob>();
        await Task.Delay(5); // ensure CreatedAt differs
        await sched.EnqueueAsync<PriorityOneJob>();

        var pending = await repo.GetPendingJobsAsync(10);
        Assert.Equal(2, pending.Count);
        Assert.True(pending[0].Priority <= pending[1].Priority);
    }
}
