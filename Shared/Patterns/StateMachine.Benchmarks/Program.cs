using System.Diagnostics;
using Usm.Shared.Patterns.StateMachine;

var machine = StateMachine<OrderState, OrderTrigger>.CreateBuilder()
    .Configure(OrderState.Draft, state => state.Permit(OrderTrigger.Submit, OrderState.Submitted))
    .Configure(OrderState.Submitted, state => state.Permit(OrderTrigger.Reset, OrderState.Draft))
    .Build(OrderState.Draft);

var toggle = false;
Measure("Fire", 1_000_000, () =>
{
    machine.Fire(toggle ? OrderTrigger.Reset : OrderTrigger.Submit);
    toggle = !toggle;
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

internal enum OrderState
{
    Draft,
    Submitted
}

internal enum OrderTrigger
{
    Submit,
    Reset
}
