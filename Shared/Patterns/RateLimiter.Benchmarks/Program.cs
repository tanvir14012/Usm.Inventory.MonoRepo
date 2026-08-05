using System.Diagnostics;
using Usm.Shared.Patterns.RateLimiter;
using Usm.Shared.Patterns.RateLimiter.Abstractions;
using Usm.Shared.Patterns.RateLimiter.Builders;

var limiter = new RateLimiterBuilder<string>()
    .WithAlgorithm(RateLimiterAlgorithm.TokenBucket)
    .WithPermitLimit(1000)
    .WithWindow(TimeSpan.FromSeconds(1))
    .Build();

Measure("Acquire", 1_000_000, async () => await limiter.AcquireAsync("bench"));

static void Measure(string name, int iterations, Func<Task> action)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var before = GC.GetAllocatedBytesForCurrentThread();
    var sw = Stopwatch.StartNew();

    for (var i = 0; i < iterations; i++)
        action().GetAwaiter().GetResult();

    sw.Stop();
    var after = GC.GetAllocatedBytesForCurrentThread();

    Console.WriteLine($"{name}: {sw.ElapsedMilliseconds} ms, alloc={(after - before):n0} bytes");
}
