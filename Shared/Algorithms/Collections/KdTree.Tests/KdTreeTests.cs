using Usm.Shared.Algorithms.Collections.KdTree.Extensions;
using Xunit;

namespace Usm.Shared.Algorithms.Collections.KdTree.Tests;

public sealed class KdTreeTests
{
    [Fact]
    public void FindsNearestNeighbor()
    {
        var tree = KdTreeExtensions.CreateBuilder<double, string>().Build();
        tree.Add(0, 0, "origin");
        tree.Add(10, 10, "far");
        tree.Add(2, 1, "near");

        var nearest = tree.NearestNeighbor(1.8, 1.2);

        Assert.NotNull(nearest);
        Assert.Equal("near", nearest.Value.Value);
    }

    [Fact]
    public void QueriesRange()
    {
        var tree = KdTreeExtensions.CreateBuilder<double, int>().Build();
        tree.Add(1, 1, 1);
        tree.Add(5, 5, 2);
        tree.Add(3, 4, 3);

        var results = tree.QueryRange(0, 0, 4, 4);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void RemovesPoints()
    {
        var tree = KdTreeExtensions.CreateBuilder<double, int>().Build();
        tree.Add(1, 1, 1);

        Assert.True(tree.Remove(1, 1, 1));
        Assert.Equal(0, tree.Count);
    }

    [Fact]
    public async Task SupportsAsyncOperations()
    {
        var tree = KdTreeExtensions.CreateBuilder<double, int>().Build();
        await tree.AddAsync(2, 3, 1);

        var nearest = await tree.NearestNeighborAsync(2.1, 3.2);

        Assert.NotNull(nearest);
    }

    [Fact]
    public void ClearsState()
    {
        var tree = KdTreeExtensions.CreateBuilder<double, int>().Build();
        tree.Add(1, 1, 1);
        tree.Clear();

        Assert.Equal(0, tree.Count);
        Assert.Empty(tree.QueryRange(-1, -1, 2, 2));
    }
}
