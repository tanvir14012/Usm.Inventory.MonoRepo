using System.Numerics;
using Usm.Shared.Algorithms.Collections.SegmentTree.Abstractions;

namespace Usm.Shared.Algorithms.Collections.SegmentTree.Builders;

/// <summary>
/// Fluent builder for segment tree configuration.
/// </summary>
/// <typeparam name="T">The numeric type.</typeparam>
public sealed class SegmentTreeBuilder<T> : ISegmentTreeBuilder<T>
    where T : INumber<T>
{
    private int _length = 16;

    /// <inheritdoc />
    public ISegmentTreeBuilder<T> WithLength(int length)
    {
        _length = length > 0 ? length : throw new ArgumentOutOfRangeException(nameof(length));
        return this;
    }

    /// <inheritdoc />
    public ISegmentTree<T> Build() => new SegmentTree<T>(_length);
}
