using Usm.Shared.Algorithms.Collections.Trie.Abstractions;

namespace Usm.Shared.Algorithms.Collections.Trie.Builders;

/// <summary>
/// Fluent builder for trie configuration.
/// </summary>
/// <typeparam name="TValue">The stored value type.</typeparam>
public sealed class TrieBuilder<TValue> : ITrieBuilder<TValue>
{
    private StringComparer _comparer = StringComparer.Ordinal;

    /// <inheritdoc />
    public ITrieBuilder<TValue> WithComparer(StringComparer comparer)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        return this;
    }

    /// <inheritdoc />
    public ITrie<TValue> Build() => new Trie<TValue>(_comparer);
}
