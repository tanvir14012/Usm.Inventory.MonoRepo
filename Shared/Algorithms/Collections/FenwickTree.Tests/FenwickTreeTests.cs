using Usm.Shared.Algorithms.Collections.FenwickTree.Extensions;
using Xunit;

namespace Usm.Shared.Algorithms.Collections.FenwickTree.Tests;

public sealed class FenwickTreeTests
{
    [Fact]
    public void ComputesPrefixAndRangeSums()
    {
        var tree = FenwickTreeExtensions.CreateBuilder<int>().WithLength(8).Build();
        tree.Add(0, 5);
        tree.Add(3, 2);
        tree.Add(4, 7);

        Assert.Equal(5, tree.PrefixSum(0));
        Assert.Equal(7, tree.PrefixSum(3));
        Assert.Equal(9, tree.RangeSum(3, 4));
    }

    [Fact]
    public async Task SupportsAsyncOperations()
    {
        var tree = FenwickTreeExtensions.CreateBuilder<long>().WithLength(4).Build();
        await tree.AddAsync(1, 3);

        Assert.Equal(3L, await tree.PrefixSumAsync(1));
    }

    [Fact]
    public void ClearsState()
    {
        var tree = FenwickTreeExtensions.CreateBuilder<int>().WithLength(4).Build();
        tree.Add(1, 2);
        tree.Clear();

        Assert.Equal(0, tree.PrefixSum(3));
    }
}
