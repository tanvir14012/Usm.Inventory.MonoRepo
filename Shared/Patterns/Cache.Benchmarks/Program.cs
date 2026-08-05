using System.Diagnostics;
using Usm.Shared.Patterns.Cache;

var cache = Cache<string, string>.CreateBuilder()
    .UseLru()
    .WithCapacity(1024)
    .Build();

await cache.SetAsync("a", "1");

Measure("Get", 1_000_000, () => cache.TryGetValue("a", out _));
Measure("Set", 1_000_000, () => cache.SetAsync("b", "2").GetAwaiter().GetResult());

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
