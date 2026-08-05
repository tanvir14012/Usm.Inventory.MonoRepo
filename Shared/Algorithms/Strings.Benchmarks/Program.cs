using System.Diagnostics;
using Usm.Shared.Algorithms.Strings.Extensions;

var alg = StringAlgorithmsExtensions.CreateBuilder().Build();
var text = new string('a', 8192) + "needle";
var pattern = "needle";

Measure("Strings", 10_000, () =>
{
    alg.KmpSearch(text, pattern);
    alg.LevenshteinDistance("kitten", "sitting");
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
