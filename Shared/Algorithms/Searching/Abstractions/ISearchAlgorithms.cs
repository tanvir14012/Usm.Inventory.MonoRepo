using System.Collections.Generic;

namespace Usm.Shared.Algorithms.Searching.Abstractions;

/// <summary>
/// Represents reusable search algorithms for sorted sequences.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public interface ISearchAlgorithms<T>
    where T : notnull
{
    /// <summary>Gets the comparer used to order values.</summary>
    IComparer<T> Comparer { get; }

    /// <summary>Performs a binary search.</summary>
    int BinarySearch(IReadOnlyList<T> items, T target);

    /// <summary>Performs a binary search asynchronously.</summary>
    ValueTask<int> BinarySearchAsync(IReadOnlyList<T> items, T target, CancellationToken cancellationToken = default);

    /// <summary>Performs a jump search.</summary>
    int JumpSearch(IReadOnlyList<T> items, T target);

    /// <summary>Performs a jump search asynchronously.</summary>
    ValueTask<int> JumpSearchAsync(IReadOnlyList<T> items, T target, CancellationToken cancellationToken = default);

    /// <summary>Performs an exponential search.</summary>
    int ExponentialSearch(IReadOnlyList<T> items, T target);

    /// <summary>Performs an exponential search asynchronously.</summary>
    ValueTask<int> ExponentialSearchAsync(IReadOnlyList<T> items, T target, CancellationToken cancellationToken = default);
}
