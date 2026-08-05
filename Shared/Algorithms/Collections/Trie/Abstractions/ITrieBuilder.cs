namespace Usm.Shared.Algorithms.Collections.Trie.Abstractions;

/// <summary>
/// Fluent builder for trie configuration.
/// </summary>
/// <typeparam name="TValue">The stored value type.</typeparam>
public interface ITrieBuilder<TValue>
{
    /// <summary>Sets the string comparer.</summary>
    ITrieBuilder<TValue> WithComparer(StringComparer comparer);

    /// <summary>Builds the trie.</summary>
    ITrie<TValue> Build();
}
