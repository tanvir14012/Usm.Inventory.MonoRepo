using System.Diagnostics;
using Usm.Shared.Patterns.RuleEngine;

var engine = RuleEngine<OrderContext, string>.CreateBuilder()
    .WhenPredicate(ctx => ctx.Amount >= 100, _ => "High", priority: 10)
    .WhenPredicate(ctx => ctx.Amount >= 50, _ => "Medium", priority: 5)
    .OtherwisePredicate(_ => "Low")
    .Build();

var context = new OrderContext(120m);

Measure("Evaluate", 1_000_000, () => engine.Evaluate(context));
Measure("Compile", 100_000, () => engine.Compile());
Measure("Expression", 100_000, () => engine.ToExpression());

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

internal sealed record OrderContext(decimal Amount);
