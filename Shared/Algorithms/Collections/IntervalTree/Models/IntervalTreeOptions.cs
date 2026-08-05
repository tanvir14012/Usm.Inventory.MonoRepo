namespace Usm.Shared.Algorithms.Collections.IntervalTree;

/// <summary>
/// Configuration for interval tree instances.
/// </summary>
/// <typeparam name="TKey">The interval boundary type.</typeparam>
public sealed class IntervalTreeOptions<TKey>
    where TKey : notnull
{
    /// <summary>Gets or sets the comparer used to order boundaries.</summary>
    public IComparer<TKey> Comparer { get; set; } = Comparer<TKey>.Default;
}
