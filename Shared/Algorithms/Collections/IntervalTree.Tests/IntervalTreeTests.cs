using Usm.Shared.Algorithms.Collections.IntervalTree.Extensions;
using Xunit;

namespace Usm.Shared.Algorithms.Collections.IntervalTree.Tests;

public sealed class IntervalTreeTests
{
    [Fact]
    public void FindsOverlappingIntervals()
    {
        var tree = IntervalTreeExtensions.CreateBuilder<int, string>().Build();
        tree.Add(0, 10, "a");
        tree.Add(12, 20, "b");
        tree.Add(8, 15, "c");

        var results = tree.QueryOverlapping(9, 13);

        Assert.Equal(3, results.Count);
        Assert.Contains(results, interval => interval.Value == "a");
        Assert.Contains(results, interval => interval.Value == "b");
        Assert.Contains(results, interval => interval.Value == "c");
    }

    [Fact]
    public void SupportsPointQueries()
    {
        var tree = IntervalTreeExtensions.CreateBuilder<int, int>().Build();
        tree.Add(3, 7, 1);
        tree.Add(7, 9, 2);

        var results = tree.QueryPoint(7);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void RemovesExactIntervals()
    {
        var tree = IntervalTreeExtensions.CreateBuilder<int, string>().Build();
        tree.Add(1, 5, "x");

        Assert.True(tree.Remove(1, 5, "x"));
        Assert.Empty(tree.QueryPoint(3));
    }

    [Fact]
    public async Task SupportsAsyncOperations()
    {
        var tree = IntervalTreeExtensions.CreateBuilder<int, string>().Build();
        await tree.AddAsync(2, 4, "x");

        Assert.True(await tree.ContainsOverlapAsync(3, 3));
    }

    [Fact]
    public void ClearsState()
    {
        var tree = IntervalTreeExtensions.CreateBuilder<int, string>().Build();
        tree.Add(1, 2, "x");
        tree.Clear();

        Assert.Equal(0, tree.Count);
        Assert.Empty(tree.QueryPoint(1));
    }
}
