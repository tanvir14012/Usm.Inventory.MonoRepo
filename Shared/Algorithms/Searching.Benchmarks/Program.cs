using System.Diagnostics;
using Usm.Shared.Algorithms.Searching.Extensions;

var algorithms = SearchAlgorithmsExtensions.CreateBuilder<int>().Build();
var values = Enumerable.Range(0, 8192).ToArray();

Measure("Searching", 1_000_000, () =>
{
    algorithms.BinarySearch(values, 4096);
    algorithms.JumpSearch(values, 4096);
    algorithms.ExponentialSearch(values, 4096);
    algorithms.InterpolationSearch(values, 4096);
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
