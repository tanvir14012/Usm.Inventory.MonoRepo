using System.Collections.Generic;
using Usm.Shared.Algorithms.Collections.IntervalTree;

namespace Usm.Shared.Algorithms.Collections.IntervalTree.Abstractions;

/// <summary>
/// Represents a reusable interval tree.
/// </summary>
/// <typeparam name="TKey">The interval boundary type.</typeparam>
/// <typeparam name="TValue">The stored payload type.</typeparam>
public interface IIntervalTree<TKey, TValue>
    where TKey : notnull
{
    /// <summary>Gets the comparer used to order boundaries.</summary>
    IComparer<TKey> Comparer { get; }

    /// <summary>Gets the number of stored intervals.</summary>
    int Count { get; }

    /// <summary>Adds an interval.</summary>
    void Add(TKey start, TKey end, TValue value);

    /// <summary>Adds an interval asynchronously.</summary>
    ValueTask AddAsync(TKey start, TKey end, TValue value, CancellationToken cancellationToken = default);

    /// <summary>Removes an exact interval match.</summary>
    bool Remove(TKey start, TKey end, TValue value);

    /// <summary>Removes an exact interval match asynchronously.</summary>
    ValueTask<bool> RemoveAsync(TKey start, TKey end, TValue value, CancellationToken cancellationToken = default);

    /// <summary>Finds all intervals overlapping a range.</summary>
    IReadOnlyList<Interval<TKey, TValue>> QueryOverlapping(TKey start, TKey end);

    /// <summary>Finds all intervals overlapping a range asynchronously.</summary>
    ValueTask<IReadOnlyList<Interval<TKey, TValue>>> QueryOverlappingAsync(TKey start, TKey end, CancellationToken cancellationToken = default);

    /// <summary>Finds all intervals containing a point.</summary>
    IReadOnlyList<Interval<TKey, TValue>> QueryPoint(TKey point);

    /// <summary>Finds all intervals containing a point asynchronously.</summary>
    ValueTask<IReadOnlyList<Interval<TKey, TValue>>> QueryPointAsync(TKey point, CancellationToken cancellationToken = default);

    /// <summary>Determines whether any interval overlaps a range.</summary>
    bool ContainsOverlap(TKey start, TKey end);

    /// <summary>Determines whether any interval overlaps a range asynchronously.</summary>
    ValueTask<bool> ContainsOverlapAsync(TKey start, TKey end, CancellationToken cancellationToken = default);

    /// <summary>Clears all intervals.</summary>
    void Clear();
}
