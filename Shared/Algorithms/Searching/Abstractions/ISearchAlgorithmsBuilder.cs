namespace Usm.Shared.Algorithms.Searching.Abstractions;

/// <summary>
/// Builds search algorithm instances.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public interface ISearchAlgorithmsBuilder<T>
    where T : notnull
{
    /// <summary>Configures the comparer.</summary>
    ISearchAlgorithmsBuilder<T> WithComparer(IComparer<T> comparer);

    /// <summary>Builds the algorithm set.</summary>
    ISearchAlgorithms<T> Build();
}
