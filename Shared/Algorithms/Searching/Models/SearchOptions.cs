namespace Usm.Shared.Algorithms.Searching;

/// <summary>
/// Configuration for search algorithms.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class SearchOptions<T>
    where T : notnull
{
    /// <summary>Gets or sets the comparer.</summary>
    public IComparer<T> Comparer { get; set; } = Comparer<T>.Default;
}
