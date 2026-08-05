using Usm.Shared.Algorithms.Collections.SegmentTree.Extensions;
using Xunit;

namespace Usm.Shared.Algorithms.Collections.SegmentTree.Tests;

public sealed class SegmentTreeTests
{
    [Fact]
    public void QueriesRangeSums()
    {
        var tree = SegmentTreeExtensions.CreateBuilder<int>().WithLength(8).Build();
        tree.Add(0, 5);
        tree.Add(3, 2);
        tree.Add(4, 7);

        Assert.Equal(14, tree.Query(0, 4));
        Assert.Equal(9, tree.Query(3, 4));
    }

    [Fact]
    public async Task SupportsAsyncOperations()
    {
        var tree = SegmentTreeExtensions.CreateBuilder<long>().WithLength(4).Build();
        await tree.AddAsync(1, 3);

        Assert.Equal(3L, await tree.QueryAsync(0, 2));
    }

    [Fact]
    public void ClearsState()
    {
        var tree = SegmentTreeExtensions.CreateBuilder<int>().WithLength(4).Build();
        tree.Add(1, 2);
        tree.Clear();

        Assert.Equal(0, tree.Query(0, 3));
    }
}
