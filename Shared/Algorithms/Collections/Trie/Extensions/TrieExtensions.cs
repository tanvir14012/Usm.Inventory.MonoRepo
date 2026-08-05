using Usm.Shared.Algorithms.Collections.Trie.Abstractions;
using Usm.Shared.Algorithms.Collections.Trie.Builders;

namespace Usm.Shared.Algorithms.Collections.Trie.Extensions;

/// <summary>
/// Common extension methods for trie creation.
/// </summary>
public static class TrieExtensions
{
    /// <summary>Creates a new trie builder.</summary>
    public static ITrieBuilder<TValue> CreateBuilder<TValue>() => new TrieBuilder<TValue>();
}
