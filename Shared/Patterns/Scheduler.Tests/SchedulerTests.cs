using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.Scheduler.Abstractions;
using Usm.Shared.Patterns.Scheduler.Builders;
using Usm.Shared.Patterns.Scheduler.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.Scheduler.Tests;

public sealed class SchedulerTests
{
    [Fact]
    public async Task ExecutesDelayedJobsInPriorityOrder()
    {
        var executed = new List<string>();
        var timeProvider = new FixedTimeProvider(DateTimeOffset.UtcNow);
        var scheduler = new SchedulerBuilder<string>()
            .WithTimeProvider(timeProvider)
            .WithHandler(new DelegateHandler(message => executed.Add(message)))
            .Build();

        await scheduler.ScheduleDelayedAsync("low", TimeSpan.Zero, priority: 0);
        await scheduler.ScheduleDelayedAsync("high", TimeSpan.Zero, priority: 10);

        var count = await scheduler.RunDueAsync();

        Assert.Equal(2, count);
        Assert.Equal(new[] { "high", "low" }, executed);
    }

    [Fact]
    public async Task ReschedulesRecurringJobs()
    {
        var executed = 0;
        var now = DateTimeOffset.UtcNow;
        var timeProvider = new FixedTimeProvider(now);
        var scheduler = new SchedulerBuilder<string>()
            .WithTimeProvider(timeProvider)
            .WithHandler(new DelegateHandler(_ => executed++))
            .Build();

        await scheduler.ScheduleRecurringAsync("tick", TimeSpan.FromMinutes(1));
        await scheduler.RunDueAsync();

        Assert.Equal(1, executed);
        Assert.Equal(1, scheduler.PendingCount);
    }

    [Fact]
    public async Task ParsesCronSchedules()
    {
        Assert.True(CronSchedule.TryParse("*/5 * * * *", out var schedule));
        Assert.NotNull(schedule);

        var next = schedule!.GetNextRun(new DateTimeOffset(2026, 1, 1, 10, 3, 0, TimeSpan.Zero));

        Assert.Equal(new DateTimeOffset(2026, 1, 1, 10, 5, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddSchedulerFramework();

        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IJobStore<string>>();

        Assert.NotNull(store);
    }

    private sealed class DelegateHandler : IJobHandler<string>
    {
        private readonly Action<string> _action;

        public DelegateHandler(Action<string> action)
        {
            _action = action;
        }

        public ValueTask HandleAsync(string job, CancellationToken cancellationToken = default)
        {
            _action(job);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
