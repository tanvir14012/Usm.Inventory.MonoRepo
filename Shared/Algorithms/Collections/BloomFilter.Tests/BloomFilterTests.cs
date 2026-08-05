using Usm.Shared.Algorithms.Collections.BloomFilter.Builders;
using Xunit;

namespace Usm.Shared.Algorithms.Collections.BloomFilter.Tests;

public sealed class BloomFilterTests
{
    [Fact]
    public void AddsAndDetectsMembers()
    {
        var filter = new BloomFilterBuilder<string>()
            .WithExpectedItemCount(128)
            .WithFalsePositiveRate(0.01)
            .Build();

        filter.Add("hello");

        Assert.True(filter.MightContain("hello"));
    }

    [Fact]
    public void ClearsMembershipState()
    {
        var filter = new BloomFilterBuilder<string>().Build();
        filter.Add("hello");
        filter.Clear();

        Assert.False(filter.MightContain("hello"));
        Assert.Equal(0, filter.Count);
    }

    [Fact]
    public async Task SupportsAsyncOperations()
    {
        var filter = new BloomFilterBuilder<int>().Build();
        await filter.AddAsync(1);

        Assert.True(await filter.MightContainAsync(1));
    }
}
