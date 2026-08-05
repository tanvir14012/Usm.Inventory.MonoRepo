using System.Diagnostics;
using Usm.Shared.Patterns.Strategy;
using Usm.Shared.Patterns.Strategy.Extensions;

var strategy = Strategy<PriceContext, decimal>.From(ctx => ctx.BasePrice * (1 - ctx.Discount));
var context = new PriceContext(100m, 0.15m);

Measure("Execute", 1_000_000, () => strategy.Execute(context));
Measure("Compile", 100_000, () => strategy.Compile());
Measure("Expression", 100_000, () => strategy.ToExpression());

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

internal sealed record PriceContext(decimal BasePrice, decimal Discount);
