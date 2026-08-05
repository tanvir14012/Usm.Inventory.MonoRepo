using Usm.Shared.Algorithms.Collections.Trie.Extensions;
using Xunit;

namespace Usm.Shared.Algorithms.Collections.Trie.Tests;

public sealed class TrieTests
{
    [Fact]
    public void AddsAndRetrievesValues()
    {
        var trie = TrieExtensions.CreateBuilder<int>().Build();
        trie.Add("cat", 1);

        Assert.True(trie.TryGetValue("cat", out var value));
        Assert.Equal(1, value);
    }

    [Fact]
    public void ReturnsPrefixMatches()
    {
        var trie = TrieExtensions.CreateBuilder<int>().Build();
        trie.Add("car", 1);
        trie.Add("cat", 2);

        var matches = trie.GetPrefixMatches("ca").ToArray();

        Assert.Equal(2, matches.Length);
        Assert.Contains(matches, pair => pair.Key == "car" && pair.Value == 1);
        Assert.Contains(matches, pair => pair.Key == "cat" && pair.Value == 2);
    }

    [Fact]
    public void RemovesKeys()
    {
        var trie = TrieExtensions.CreateBuilder<int>().Build();
        trie.Add("cat", 1);

        Assert.True(trie.Remove("cat"));
        Assert.False(trie.ContainsKey("cat"));
        Assert.Equal(0, trie.Count);
    }
}
