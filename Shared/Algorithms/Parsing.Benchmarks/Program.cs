using System.Diagnostics;
using Usm.Shared.Algorithms.Parsing.Extensions;

var alg = ParsingAlgorithmsExtensions.CreateBuilder().Build();

Measure("Parsing", 10_000, () =>
{
    alg.ShuntingYard("3+4*2");
    alg.RecursiveDescentParse("5+3*2");
    alg.EvaluatePostfix("32+4*");
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
