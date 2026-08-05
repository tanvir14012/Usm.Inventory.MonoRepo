using System.Numerics;

namespace Usm.Shared.Algorithms.Collections.SegmentTree.Abstractions;

/// <summary>
/// Fluent builder for segment tree configuration.
/// </summary>
/// <typeparam name="T">The numeric type.</typeparam>
public interface ISegmentTreeBuilder<T>
    where T : INumber<T>
{
    /// <summary>Sets the tree length.</summary>
    ISegmentTreeBuilder<T> WithLength(int length);

    /// <summary>Builds the tree.</summary>
    ISegmentTree<T> Build();
}
