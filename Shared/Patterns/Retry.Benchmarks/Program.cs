using System.Diagnostics;
using Usm.Shared.Patterns.Retry;
using Usm.Shared.Patterns.Retry.Builders;

var policy = new RetryBuilder<int, int>()
    .WithMaxAttempts(3)
    .WithDelay(TimeSpan.Zero)
    .Build();

Measure("Execute", 1_000_000, () => policy.Execute(1, value => value + 1));

static void Measure(string name, int iterations, Action action)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var before = GC.GetAllocatedBytesForCurrentThread();
    var sw = Stopwatch.StartNew();

    for (var i = 0; i < iterations; i++)
        action();

    sw.Stop();
    var after = GC.GetAllocatedBytesForCurrentThread();

    Console.WriteLine($"{name}: {sw.ElapsedMilliseconds} ms, alloc={(after - before):n0} bytes");
}
