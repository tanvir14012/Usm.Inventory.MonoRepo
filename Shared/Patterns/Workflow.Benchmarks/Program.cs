using System.Diagnostics;
using Usm.Shared.Patterns.Workflow;

var workflow = Workflow<OrderContext>.CreateBuilder()
    .Then(ctx => ctx with { Approved = true })
    .Then(ctx => ctx with { Total = ctx.Subtotal + ctx.Tax })
    .Build();

var context = new OrderContext(100m, 15m, false);

Measure("Execute", 1_000_000, () => workflow.Execute(context));

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

internal sealed record OrderContext(decimal Subtotal, decimal Tax, bool RequiresApproval)
{
    public decimal Total { get; init; }
    public bool Approved { get; init; }
}
