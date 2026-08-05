using System.Numerics;

namespace Usm.Shared.Algorithms.Collections.KdTree.Abstractions;

/// <summary>
/// Builds KD-tree instances.
/// </summary>
/// <typeparam name="TCoordinate">The coordinate type.</typeparam>
/// <typeparam name="TValue">The payload type.</typeparam>
public interface IKdTreeBuilder<TCoordinate, TValue>
    where TCoordinate : IFloatingPointIeee754<TCoordinate>
{
    /// <summary>Configures duplicate point handling.</summary>
    IKdTreeBuilder<TCoordinate, TValue> WithAllowDuplicatePoints(bool allowDuplicatePoints);

    /// <summary>Builds the tree.</summary>
    IKdTree<TCoordinate, TValue> Build();
}
