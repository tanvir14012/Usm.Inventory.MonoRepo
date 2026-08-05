using System.Diagnostics;
using Usm.Shared.Algorithms.Collections.SegmentTree.Extensions;

var tree = SegmentTreeExtensions.CreateBuilder<int>().WithLength(1024).Build();

Measure("SegmentTree", 1_000_000, () =>
{
    tree.Add(1, 1);
    tree.Query(0, 1);
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
