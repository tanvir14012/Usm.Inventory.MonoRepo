using System.Numerics;
using Usm.Shared.Algorithms.Collections.KdTree.Abstractions;

namespace Usm.Shared.Algorithms.Collections.KdTree.Builders;

/// <summary>
/// Fluent builder for KD-trees.
/// </summary>
/// <typeparam name="TCoordinate">The coordinate type.</typeparam>
/// <typeparam name="TValue">The payload type.</typeparam>
public sealed class KdTreeBuilder<TCoordinate, TValue> : IKdTreeBuilder<TCoordinate, TValue>
    where TCoordinate : IFloatingPointIeee754<TCoordinate>
{
    private readonly KdTreeOptions<TCoordinate> _options = new();

    /// <inheritdoc />
    public IKdTreeBuilder<TCoordinate, TValue> WithAllowDuplicatePoints(bool allowDuplicatePoints)
    {
        _options.AllowDuplicatePoints = allowDuplicatePoints;
        return this;
    }

    /// <inheritdoc />
    public IKdTree<TCoordinate, TValue> Build() => new KdTree<TCoordinate, TValue>(_options);
}
