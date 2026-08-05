using Usm.Shared.Algorithms.Collections.BTree.Abstractions;

namespace Usm.Shared.Algorithms.Collections.BTree.Builders;

/// <summary>
/// Fluent builder for B-trees.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public sealed class BTreeBuilder<TKey, TValue> : IBTreeBuilder<TKey, TValue>
    where TKey : notnull
{
    private readonly BTreeOptions<TKey> _options = new();

    /// <inheritdoc />
    public IBTreeBuilder<TKey, TValue> WithComparer(IComparer<TKey> comparer)
    {
        _options.Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        return this;
    }

    /// <inheritdoc />
    public IBTreeBuilder<TKey, TValue> WithMinimumDegree(int minimumDegree)
    {
        _options.MinimumDegree = minimumDegree >= 2 ? minimumDegree : throw new ArgumentOutOfRangeException(nameof(minimumDegree));
        return this;
    }

    /// <inheritdoc />
    public IBTree<TKey, TValue> Build() => new BTree<TKey, TValue>(_options);
}
