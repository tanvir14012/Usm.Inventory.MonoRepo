using System.Diagnostics;
using Usm.Shared.Algorithms.Compression.Extensions;

var alg = CompressionAlgorithmsExtensions.CreateBuilder().Build();

Measure("Compression", 10_000, () =>
{
    alg.RunLengthEncode("aaabbbccc");
    alg.DeltaEncode(new byte[] { 1, 2, 3, 4, 5 });
    alg.HuffmanEncode("hello");
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
