namespace Usm.Shared.Algorithms.Collections.IntervalTree.Abstractions;

/// <summary>
/// Builds interval tree instances.
/// </summary>
/// <typeparam name="TKey">The interval boundary type.</typeparam>
/// <typeparam name="TValue">The stored payload type.</typeparam>
public interface IIntervalTreeBuilder<TKey, TValue>
    where TKey : notnull
{
    /// <summary>Configures the comparer used for ordering.</summary>
    IIntervalTreeBuilder<TKey, TValue> WithComparer(IComparer<TKey> comparer);

    /// <summary>Builds the tree.</summary>
    IIntervalTree<TKey, TValue> Build();
}
