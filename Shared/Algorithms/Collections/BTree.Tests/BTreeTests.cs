using Usm.Shared.Algorithms.Collections.BTree.Extensions;
using Xunit;

namespace Usm.Shared.Algorithms.Collections.BTree.Tests;

public sealed class BTreeTests
{
    [Fact]
    public void InsertsAndFindsKeys()
    {
        var tree = BTreeExtensions.CreateBuilder<int, string>().WithMinimumDegree(2).Build();
        tree.Add(10, "a");
        tree.Add(20, "b");
        tree.Add(5, "c");
        tree.Add(6, "d");

        Assert.True(tree.ContainsKey(20));
        Assert.True(tree.TryGetValue(5, out var value));
        Assert.Equal("c", value);
    }

    [Fact]
    public void OverwritesExistingKeys()
    {
        var tree = BTreeExtensions.CreateBuilder<int, string>().Build();
        tree.Add(1, "a");
        tree.Add(1, "b");

        Assert.Equal(1, tree.Count);
        Assert.True(tree.TryGetValue(1, out var value));
        Assert.Equal("b", value);
    }

    [Fact]
    public void TraversesInOrder()
    {
        var tree = BTreeExtensions.CreateBuilder<int, string>().WithMinimumDegree(2).Build();
        tree.Add(3, "c");
        tree.Add(1, "a");
        tree.Add(2, "b");
        tree.Add(4, "d");

        var keys = tree.Traverse().Select(pair => pair.Key).ToArray();

        Assert.Equal(new[] { 1, 2, 3, 4 }, keys);
    }

    [Fact]
    public async Task SupportsAsyncOperations()
    {
        var tree = BTreeExtensions.CreateBuilder<int, string>().Build();
        await tree.AddAsync(7, "x");

        Assert.True(await tree.ContainsKeyAsync(7));
        var result = await tree.TryGetValueAsync(7);
        Assert.True(result.Found);
        Assert.Equal("x", result.Value);
    }

    [Fact]
    public void ClearsState()
    {
        var tree = BTreeExtensions.CreateBuilder<int, string>().Build();
        tree.Add(1, "x");
        tree.Clear();

        Assert.Equal(0, tree.Count);
        Assert.Empty(tree.Traverse());
    }
}
