using Usm.Shared.Algorithms.Strings.Extensions;
using Xunit;

namespace Usm.Shared.Algorithms.Strings.Tests;

public sealed class StringAlgorithmsTests
{
    [Fact]
    public void PerformsStringMatching()
    {
        var alg = StringAlgorithmsExtensions.CreateBuilder().Build();

        Assert.Equal(0, alg.KmpSearch("abcdef", "abc"));
        Assert.Equal(3, alg.RabinKarpSearch("abcdef", "def"));
        Assert.Equal(1, alg.BoyerMooreSearch("abcdef", "bcd"));
        Assert.Equal(-1, alg.KmpSearch("abcdef", "xyz"));
    }

    [Fact]
    public void ComputesStringDistances()
    {
        var alg = StringAlgorithmsExtensions.CreateBuilder().Build();

        Assert.Equal(1, alg.LevenshteinDistance("cat", "bat"));
        Assert.Equal(2, alg.DamerauLevenshteinDistance("ca", "ac"));
        Assert.Equal(3, alg.LongestCommonSubsequenceLength("abcdef", "fbdamn"));
    }

    [Fact]
    public async Task SupportsAsyncOperations()
    {
        var alg = StringAlgorithmsExtensions.CreateBuilder().Build();

        Assert.Equal(0, await alg.KmpSearchAsync("test", "test"));
        Assert.Equal(3, await alg.LevenshteinDistanceAsync("kitten", "sitting"));
    }
}
