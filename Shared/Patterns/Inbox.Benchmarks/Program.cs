using System.Diagnostics;
using Usm.Shared.Patterns.Inbox;
using Usm.Shared.Patterns.Inbox.Abstractions;
using Usm.Shared.Patterns.Inbox.Builders;

var processed = 0;
var inbox = new InboxBuilder<string, string>()
    .WithKeySelector(message => message)
    .WithHandler(new DelegateHandler(_ => Interlocked.Increment(ref processed)))
    .Build();

Measure("Process+Duplicate", 500_000, async () =>
{
    await inbox.ProcessAsync("key");
    await inbox.ProcessAsync("key");
    await inbox.CleanupExpiredAsync();
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

internal sealed class DelegateHandler : IInboxHandler<string>
{
    private readonly Action<string> _action;

    public DelegateHandler(Action<string> action)
    {
        _action = action;
    }

    public ValueTask HandleAsync(string message, CancellationToken cancellationToken = default)
    {
        _action(message);
        return ValueTask.CompletedTask;
    }
}
