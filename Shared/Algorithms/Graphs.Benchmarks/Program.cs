using System.Diagnostics;
using Usm.Shared.Algorithms.Graphs.Extensions;

var graph = GraphExtensions.CreateBuilder<int, int>().WithDirected(true).Build();
for (var i = 0; i < 1024; i++)
    graph.AddEdge(i, i + 1, 1);

Measure("Graphs", 100_000, () =>
{
    graph.BreadthFirstSearch(0);
    graph.Dijkstra(0, 1024);
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
