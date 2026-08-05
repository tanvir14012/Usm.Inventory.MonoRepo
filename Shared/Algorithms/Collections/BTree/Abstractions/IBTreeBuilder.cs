namespace Usm.Shared.Algorithms.Collections.BTree.Abstractions;

/// <summary>
/// Builds B-tree instances.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public interface IBTreeBuilder<TKey, TValue>
    where TKey : notnull
{
    /// <summary>Configures the key comparer.</summary>
    IBTreeBuilder<TKey, TValue> WithComparer(IComparer<TKey> comparer);

    /// <summary>Configures the minimum degree.</summary>
    IBTreeBuilder<TKey, TValue> WithMinimumDegree(int minimumDegree);

    /// <summary>Builds the tree.</summary>
    IBTree<TKey, TValue> Build();
}
