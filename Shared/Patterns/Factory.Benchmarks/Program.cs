using System.Diagnostics;
using Usm.Shared.Patterns.Factory;
using Usm.Shared.Patterns.Factory.Extensions;

var factory = Factory<OrderContext, OrderDto>.From(ctx => new OrderDto(ctx.Id, ctx.Total));
var context = new OrderContext(1, 125m);

Measure("Create", 1_000_000, () => factory.Create(context));
Measure("Compile", 100_000, () => factory.Compile());
Measure("Expression", 100_000, () => factory.ToExpression());

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

internal sealed record OrderContext(int Id, decimal Total);

internal sealed record OrderDto(int Id, decimal Total);
