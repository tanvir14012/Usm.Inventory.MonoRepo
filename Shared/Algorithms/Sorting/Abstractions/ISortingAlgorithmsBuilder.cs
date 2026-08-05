namespace Usm.Shared.Algorithms.Sorting.Abstractions;

/// <summary>
/// Builds sorting algorithm instances.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public interface ISortingAlgorithmsBuilder<T>
    where T : notnull
{
    /// <summary>Configures the comparer.</summary>
    ISortingAlgorithmsBuilder<T> WithComparer(IComparer<T> comparer);

    /// <summary>Builds the sorting algorithm set.</summary>
    ISortingAlgorithms<T> Build();
}
