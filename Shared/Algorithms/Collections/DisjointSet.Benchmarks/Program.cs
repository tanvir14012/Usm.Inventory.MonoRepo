using System.Diagnostics;
using Usm.Shared.Algorithms.Collections.DisjointSet.Builders;

var set = new DisjointSetBuilder<int>().Build();

Measure("UnionFind", 1_000_000, () =>
{
    set.Union(1, 2);
    set.Union(2, 3);
    set.Connected(1, 3);
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
