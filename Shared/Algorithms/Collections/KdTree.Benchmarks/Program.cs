using System.Diagnostics;
using Usm.Shared.Algorithms.Collections.KdTree.Extensions;

var tree = KdTreeExtensions.CreateBuilder<double, int>().Build();
for (var i = 0; i < 1024; i++)
    tree.Add(i, i * 0.5, i);

Measure("KdTree", 1_000_000, () =>
{
    tree.NearestNeighbor(512.25, 255.75);
    tree.QueryRange(100, 100, 700, 400);
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
