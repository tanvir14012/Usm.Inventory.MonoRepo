using System.Diagnostics;
using Usm.Shared.Patterns.Outbox;
using Usm.Shared.Patterns.Outbox.Abstractions;
using Usm.Shared.Patterns.Outbox.Builders;

var dispatched = 0;
var outbox = new OutboxBuilder<string>()
    .WithDispatcher(new DelegateDispatcher(_ => Interlocked.Increment(ref dispatched)))
    .Build();

Measure("Enqueue+Dispatch", 1_000_000, async () =>
{
    await outbox.EnqueueAsync("evt");
    await outbox.DispatchPendingAsync();
});

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

internal sealed class DelegateDispatcher : IOutboxDispatcher<string>
{
    private readonly Action<string> _dispatch;

    public DelegateDispatcher(Action<string> dispatch)
    {
        _dispatch = dispatch;
    }

    public ValueTask DispatchAsync(string message, CancellationToken cancellationToken = default)
    {
        _dispatch(message);
        return ValueTask.CompletedTask;
    }
}
