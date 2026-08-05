using System.Diagnostics;
using Usm.Shared.Algorithms.Collections.PriorityQueue.Extensions;

var queue = PriorityQueueExtensions.CreateBuilder<int, int>().Build();

Measure("PriorityQueue", 1_000_000, () =>
{
    queue.Enqueue(1, 1);
    queue.Enqueue(2, 0);
    queue.Dequeue();
    queue.Dequeue();
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
