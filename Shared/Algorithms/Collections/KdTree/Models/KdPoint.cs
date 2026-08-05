using System.Numerics;

namespace Usm.Shared.Algorithms.Collections.KdTree;

/// <summary>
/// Represents a 2D point stored in a KD-tree.
/// </summary>
/// <typeparam name="TCoordinate">The coordinate type.</typeparam>
/// <typeparam name="TValue">The payload type.</typeparam>
public readonly record struct KdPoint<TCoordinate, TValue>(TCoordinate X, TCoordinate Y, TValue Value)
    where TCoordinate : IFloatingPointIeee754<TCoordinate>;
