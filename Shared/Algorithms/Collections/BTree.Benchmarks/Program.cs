using System.Diagnostics;
using Usm.Shared.Algorithms.Collections.BTree.Extensions;

var tree = BTreeExtensions.CreateBuilder<int, int>().WithMinimumDegree(8).Build();
for (var i = 0; i < 4096; i++)
    tree.Add(i, i);

Measure("BTree", 1_000_000, () =>
{
    tree.ContainsKey(2048);
    tree.TryGetValue(3000, out _);
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
