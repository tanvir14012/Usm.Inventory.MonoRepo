using System.Diagnostics;
using Usm.Shared.Patterns.EventBus.Builders;

var bus = new EventBusBuilder<string>()
    .Subscribe(_ => { })
    .Build();

Measure("Publish", 1_000_000, () => bus.Publish("evt"));

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
