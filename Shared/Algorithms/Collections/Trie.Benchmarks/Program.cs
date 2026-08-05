using System.Diagnostics;
using Usm.Shared.Algorithms.Collections.Trie.Extensions;

var trie = TrieExtensions.CreateBuilder<int>().Build();

Measure("Trie", 500_000, () =>
{
    trie.Add("cat", 1);
    trie.Add("car", 2);
    trie.ContainsKey("cat");
    trie.GetPrefixMatches("ca").ToArray();
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
