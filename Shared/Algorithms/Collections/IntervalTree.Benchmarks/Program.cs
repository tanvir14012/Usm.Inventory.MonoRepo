using System.Diagnostics;
using Usm.Shared.Algorithms.Collections.IntervalTree.Extensions;

var tree = IntervalTreeExtensions.CreateBuilder<int, int>().Build();
for (var i = 0; i < 1024; i++)
    tree.Add(i * 2, i * 2 + 1, i);

Measure("IntervalTree", 1_000_000, () =>
{
    tree.ContainsOverlap(100, 200);
    tree.QueryPoint(150);
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
