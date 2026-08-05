using System.Diagnostics;
using Usm.Shared.Algorithms.Distributed.Extensions;

var alg = DistributedAlgorithmsExtensions.CreateBuilder().Build();

Measure("Distributed", 100_000, () =>
{
    alg.ConsistentHash("key", 100);
    alg.SnowflakeId();
    var clock = new Dictionary<int, long> { { 1, 0 } };
    alg.VectorClockIncrement(clock, 1);
});

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
