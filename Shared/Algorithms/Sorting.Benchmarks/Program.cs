using System.Diagnostics;
using Usm.Shared.Algorithms.Sorting.Extensions;

var sorting = SortingAlgorithmsExtensions.CreateBuilder<int>().Build();
var source = Enumerable.Range(0, 1024).Reverse().ToArray();
var work = new int[source.Length];

Measure("Sorting", 100_000, () =>
{
    Array.Copy(source, work, source.Length);
    sorting.IntroSort(work);
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
