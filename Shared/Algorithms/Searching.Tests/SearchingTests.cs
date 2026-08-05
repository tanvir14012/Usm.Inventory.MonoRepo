using Usm.Shared.Algorithms.Searching.Extensions;
using Xunit;

namespace Usm.Shared.Algorithms.Searching.Tests;

public sealed class SearchingTests
{
    [Fact]
    public void FindsValuesWithBinaryJumpAndExponentialSearch()
    {
        var algorithms = SearchAlgorithmsExtensions.CreateBuilder<int>().Build();
        var values = new[] { 1, 3, 5, 7, 9, 11, 13, 15 };

        Assert.Equal(3, algorithms.BinarySearch(values, 7));
        Assert.Equal(5, algorithms.JumpSearch(values, 11));
        Assert.Equal(6, algorithms.ExponentialSearch(values, 13));
    }

    [Fact]
    public void SupportsInterpolationSearch()
    {
        var algorithms = SearchAlgorithmsExtensions.CreateBuilder<int>().Build();
        var values = Enumerable.Range(10, 100).ToArray();

        Assert.Equal(24, algorithms.InterpolationSearch(values, 34));
    }

    [Fact]
    public async Task SupportsAsyncOperations()
    {
        var algorithms = SearchAlgorithmsExtensions.CreateBuilder<int>().Build();
        var values = Enumerable.Range(0, 20).ToArray();

        Assert.Equal(9, await algorithms.BinarySearchAsync(values, 9));
        Assert.Equal(12, await algorithms.InterpolationSearchAsync(values, 12));
    }

    [Fact]
    public void ReturnsMinusOneForMissingValues()
    {
        var algorithms = SearchAlgorithmsExtensions.CreateBuilder<int>().Build();
        var values = new[] { 2, 4, 6, 8 };

        Assert.Equal(-1, algorithms.BinarySearch(values, 5));
        Assert.Equal(-1, algorithms.JumpSearch(values, 5));
        Assert.Equal(-1, algorithms.ExponentialSearch(values, 5));
        Assert.Equal(-1, algorithms.InterpolationSearch(values, 5));
    }
}
