using System.Diagnostics;
using Usm.Shared.Patterns.CircuitBreaker;
using Usm.Shared.Patterns.CircuitBreaker.Builders;

var breaker = new CircuitBreakerBuilder<int, int>()
    .WithFailureThreshold(2)
    .WithExecutionTimeout(null)
    .Build();

Measure("Execute", 1_000_000, () => breaker.Execute(1, value => value + 1));

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
