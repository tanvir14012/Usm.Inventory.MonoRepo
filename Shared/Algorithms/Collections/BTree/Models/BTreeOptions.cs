namespace Usm.Shared.Algorithms.Collections.BTree;

/// <summary>
/// Configuration for a B-tree.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
public sealed class BTreeOptions<TKey>
    where TKey : notnull
{
    /// <summary>Gets or sets the minimum degree.</summary>
    public int MinimumDegree { get; set; } = 16;

    /// <summary>Gets or sets the comparer.</summary>
    public IComparer<TKey> Comparer { get; set; } = Comparer<TKey>.Default;
}
