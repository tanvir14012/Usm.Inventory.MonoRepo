using System.Diagnostics;
using Usm.Shared.Patterns.Saga.Builders;

var saga = new SagaBuilder<int>()
    .WithSagaId("bench")
    .Use((ctx, ct) => ValueTask.FromResult(ctx + 1))
    .Build();

Measure("Saga", 1_000_000, async () => await saga.ExecuteAsync(1));

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
