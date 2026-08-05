using System.Collections.Generic;

namespace Usm.Shared.Algorithms.Sorting.Abstractions;

/// <summary>
/// Represents reusable sorting algorithms.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public interface ISortingAlgorithms<T>
    where T : notnull
{
    /// <summary>Gets the comparer used to order values.</summary>
    IComparer<T> Comparer { get; }

    /// <summary>Sorts values using quick sort.</summary>
    void QuickSort(T[] items);

    /// <summary>Sorts values using quick sort asynchronously.</summary>
    ValueTask QuickSortAsync(T[] items, CancellationToken cancellationToken = default);

    /// <summary>Sorts values using merge sort.</summary>
    void MergeSort(T[] items);

    /// <summary>Sorts values using merge sort asynchronously.</summary>
    ValueTask MergeSortAsync(T[] items, CancellationToken cancellationToken = default);

    /// <summary>Sorts values using heap sort.</summary>
    void HeapSort(T[] items);

    /// <summary>Sorts values using heap sort asynchronously.</summary>
    ValueTask HeapSortAsync(T[] items, CancellationToken cancellationToken = default);

    /// <summary>Sorts values using introsort.</summary>
    void IntroSort(T[] items);

    /// <summary>Sorts values using introsort asynchronously.</summary>
    ValueTask IntroSortAsync(T[] items, CancellationToken cancellationToken = default);
}
