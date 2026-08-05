using Usm.Shared.Algorithms.Collections.DisjointSet.Builders;
using Xunit;

namespace Usm.Shared.Algorithms.Collections.DisjointSet.Tests;

public sealed class DisjointSetTests
{
    [Fact]
    public void UnionsAndFindsRoots()
    {
        var set = new DisjointSetBuilder<int>().Build();
        set.Add(1);
        set.Add(2);
        set.Add(3);

        set.Union(1, 2);

        Assert.True(set.Connected(1, 2));
        Assert.False(set.Connected(1, 3));
        Assert.Equal(2, set.SetSize(1));
    }

    [Fact]
    public void PathCompressionKeepsRootsStable()
    {
        var set = new DisjointSetBuilder<int>().Build();
        set.Union(1, 2);
        set.Union(2, 3);

        var root = set.Find(3);

        Assert.Equal(set.Find(1), root);
        Assert.Equal(3, set.SetSize(1));
    }
}
