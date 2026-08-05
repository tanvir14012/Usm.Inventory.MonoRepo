using System.Diagnostics;
using Usm.Shared.Patterns.Pipeline;
using Usm.Shared.Patterns.Pipeline.Extensions;

var pipeline = Pipeline<InvoiceContext>.CreateBuilder()
    .Use(ctx => new InvoiceContext(ctx.Id, ctx.Amount + ctx.Tax, ctx.Tax))
    .Then(ctx => new InvoiceContext(ctx.Id, decimal.Round(ctx.Amount, 2), ctx.Tax))
    .Build();

var context = new InvoiceContext(1, 100m, 15m);

Measure("Execute", 1_000_000, () => pipeline.Execute(context));
Measure("Compile", 100_000, () => pipeline.Compile());
Measure("Expression", 100_000, () => pipeline.ToExpression());

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

internal sealed record InvoiceContext(int Id, decimal Amount, decimal Tax);
