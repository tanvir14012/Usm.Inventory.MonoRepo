using System.Diagnostics;
using Usm.Shared.Patterns.Scheduler.Abstractions;
using Usm.Shared.Patterns.Scheduler.Builders;

var scheduler = new SchedulerBuilder<int>()
    .WithHandler(new NoopHandler())
    .Build();

Measure("Schedule+Run", 250_000, async () =>
{
    await scheduler.ScheduleDelayedAsync(1, TimeSpan.Zero);
    await scheduler.RunDueAsync();
});

static void Measure(string name, int iterations, Func<Task> action)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var before = GC.GetAllocatedBytesForCurrentThread();
    var sw = Stopwatch.StartNew();

    for (var i = 0; i < iterations; i++)
        action().GetAwaiter().GetResult();

    sw.Stop();
    var after = GC.GetAllocatedBytesForCurrentThread();

    Console.WriteLine($"{name}: {sw.ElapsedMilliseconds} ms, alloc={(after - before):n0} bytes");
}

internal sealed class NoopHandler : IJobHandler<int>
{
    public ValueTask HandleAsync(int job, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
