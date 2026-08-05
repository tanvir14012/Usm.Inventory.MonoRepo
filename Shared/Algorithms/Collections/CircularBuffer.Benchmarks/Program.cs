using System.Diagnostics;
using Usm.Shared.Algorithms.Collections.CircularBuffer.Extensions;

var buffer = CircularBufferExtensions.CreateBuilder<int>().WithCapacity(128).Build();

Measure("CircularBuffer", 1_000_000, () =>
{
    buffer.Enqueue(1);
    buffer.TryPeek(out _);
    buffer.TryDequeue(out _);
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
