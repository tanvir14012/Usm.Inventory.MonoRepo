using System.Diagnostics;
using Usm.Shared.Patterns.Sidecar.Builders;
using Usm.Shared.Patterns.Sidecar.Configuration;

// ── Stub primary ──────────────────────────────────────────────────────────────

var primary = new NullService();
var sidecar = new SidecarBuilder<INullService>()
    .WithMaxAttempts(1)
    .WithRetryBaseDelay(TimeSpan.Zero)
    .WithJitter(false)
    .WithExecutionTimeout(null)
    .Build(primary);

// ── Benchmarks ────────────────────────────────────────────────────────────────

const int Iterations = 500_000;

await MeasureAsync("ExecuteAsync (no retry, no timeout)", Iterations, async () =>
{
    await sidecar.ExecuteAsync(static (svc, ct) => svc.ComputeAsync(ct));
});

static async Task MeasureAsync(string name, int iterations, Func<Task> action)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var before = GC.GetAllocatedBytesForCurrentThread();
    var sw = Stopwatch.StartNew();

    for (var i = 0; i < iterations; i++)
        await action();

    sw.Stop();
    var after = GC.GetAllocatedBytesForCurrentThread();

    Console.WriteLine($"{name}: {sw.ElapsedMilliseconds} ms, alloc={(after - before) / iterations:n0} bytes/call");
}

// ── Stubs ─────────────────────────────────────────────────────────────────────

public interface INullService
{
    ValueTask<int> ComputeAsync(CancellationToken ct = default);
}

public sealed class NullService : INullService
{
    public ValueTask<int> ComputeAsync(CancellationToken ct = default) => ValueTask.FromResult(42);
}
