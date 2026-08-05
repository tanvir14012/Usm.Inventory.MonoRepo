using System.Numerics;

namespace Usm.Shared.Algorithms.Collections.KdTree;

/// <summary>
/// Configuration for KD-tree instances.
/// </summary>
/// <typeparam name="TCoordinate">The coordinate type.</typeparam>
public sealed class KdTreeOptions<TCoordinate>
    where TCoordinate : IFloatingPointIeee754<TCoordinate>
{
    /// <summary>Gets or sets whether duplicate points are allowed.</summary>
    public bool AllowDuplicatePoints { get; set; } = true;
}
