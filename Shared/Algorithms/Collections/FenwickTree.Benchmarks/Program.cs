using System.Diagnostics;
using Usm.Shared.Algorithms.Collections.FenwickTree.Extensions;

var tree = FenwickTreeExtensions.CreateBuilder<int>().WithLength(1024).Build();

Measure("FenwickTree", 1_000_000, () =>
{
    tree.Add(1, 1);
    tree.PrefixSum(1);
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
