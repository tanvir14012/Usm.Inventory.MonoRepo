using System.Numerics;

namespace Usm.Shared.Algorithms.Collections.KdTree.Abstractions;

/// <summary>
/// Represents a reusable KD-tree.
/// </summary>
/// <typeparam name="TCoordinate">The coordinate type.</typeparam>
/// <typeparam name="TValue">The payload type.</typeparam>
public interface IKdTree<TCoordinate, TValue>
    where TCoordinate : IFloatingPointIeee754<TCoordinate>
{
    /// <summary>Gets the number of stored points.</summary>
    int Count { get; }

    /// <summary>Adds a point.</summary>
    void Add(TCoordinate x, TCoordinate y, TValue value);

    /// <summary>Adds a point asynchronously.</summary>
    ValueTask AddAsync(TCoordinate x, TCoordinate y, TValue value, CancellationToken cancellationToken = default);

    /// <summary>Removes a point.</summary>
    bool Remove(TCoordinate x, TCoordinate y, TValue value);

    /// <summary>Removes a point asynchronously.</summary>
    ValueTask<bool> RemoveAsync(TCoordinate x, TCoordinate y, TValue value, CancellationToken cancellationToken = default);

    /// <summary>Returns all points in a rectangular range.</summary>
    IReadOnlyList<KdPoint<TCoordinate, TValue>> QueryRange(TCoordinate minX, TCoordinate minY, TCoordinate maxX, TCoordinate maxY);

    /// <summary>Returns all points in a rectangular range asynchronously.</summary>
    ValueTask<IReadOnlyList<KdPoint<TCoordinate, TValue>>> QueryRangeAsync(TCoordinate minX, TCoordinate minY, TCoordinate maxX, TCoordinate maxY, CancellationToken cancellationToken = default);

    /// <summary>Finds the nearest point to the provided coordinate.</summary>
    KdPoint<TCoordinate, TValue>? NearestNeighbor(TCoordinate x, TCoordinate y);

    /// <summary>Finds the nearest point asynchronously.</summary>
    ValueTask<KdPoint<TCoordinate, TValue>?> NearestNeighborAsync(TCoordinate x, TCoordinate y, CancellationToken cancellationToken = default);

    /// <summary>Clears the tree.</summary>
    void Clear();
}
