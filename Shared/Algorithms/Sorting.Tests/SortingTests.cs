using Usm.Shared.Algorithms.Sorting.Extensions;
using Xunit;

namespace Usm.Shared.Algorithms.Sorting.Tests;

public sealed class SortingTests
{
    [Fact]
    public void SortsUsingComparisonAlgorithms()
    {
        var sorting = SortingAlgorithmsExtensions.CreateBuilder<int>().Build();
        var values = new[] { 5, 2, 9, 1, 7, 3 };

        sorting.QuickSort(values);
        Assert.Equal(new[] { 1, 2, 3, 5, 7, 9 }, values);

        values = new[] { 5, 2, 9, 1, 7, 3 };
        sorting.MergeSort(values);
        Assert.Equal(new[] { 1, 2, 3, 5, 7, 9 }, values);

        values = new[] { 5, 2, 9, 1, 7, 3 };
        sorting.HeapSort(values);
        Assert.Equal(new[] { 1, 2, 3, 5, 7, 9 }, values);

        values = new[] { 5, 2, 9, 1, 7, 3 };
        sorting.IntroSort(values);
        Assert.Equal(new[] { 1, 2, 3, 5, 7, 9 }, values);
    }

    [Fact]
    public void SortsWithNumericVariants()
    {
        var sorting = SortingAlgorithmsExtensions.CreateBuilder<int>().Build();
        var ints = new[] { 42, 5, 13, 9, 0, 17 };

        sorting.CountingSort(ints);
        Assert.Equal(new[] { 0, 5, 9, 13, 17, 42 }, ints);

        ints = new[] { 42, 5, 13, 9, 0, 17 };
        sorting.RadixSort(ints);
        Assert.Equal(new[] { 0, 5, 9, 13, 17, 42 }, ints);
    }

    [Fact]
    public void SortsFloatingPointValuesWithBuckets()
    {
        var sorting = SortingAlgorithmsExtensions.CreateBuilder<double>().Build();
        var values = new[] { 0.42, 0.05, 0.13, 0.09, 0.01, 0.17 };

        sorting.BucketSort(values);
        Assert.Equal(new[] { 0.01, 0.05, 0.09, 0.13, 0.17, 0.42 }, values);
    }

    [Fact]
    public async Task SupportsAsyncOperations()
    {
        var sorting = SortingAlgorithmsExtensions.CreateBuilder<int>().Build();
        var values = new[] { 3, 1, 2 };

        await sorting.QuickSortAsync(values);
        Assert.Equal(new[] { 1, 2, 3 }, values);
    }
}
