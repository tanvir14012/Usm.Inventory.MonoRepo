using System.Diagnostics;
using Usm.Shared.Algorithms.Collections.BloomFilter.Builders;

var filter = new BloomFilterBuilder<int>().WithExpectedItemCount(1000).Build();

Measure("BloomFilter", 1_000_000, () =>
{
    filter.Add(1);
    filter.MightContain(1);
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
