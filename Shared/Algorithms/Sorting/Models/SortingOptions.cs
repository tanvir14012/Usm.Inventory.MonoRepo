namespace Usm.Shared.Algorithms.Sorting;

/// <summary>
/// Configuration for sorting algorithms.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class SortingOptions<T>
    where T : notnull
{
    /// <summary>Gets or sets the comparer.</summary>
    public IComparer<T> Comparer { get; set; } = Comparer<T>.Default;
}
