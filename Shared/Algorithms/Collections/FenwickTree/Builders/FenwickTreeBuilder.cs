using System.Numerics;
using Usm.Shared.Algorithms.Collections.FenwickTree.Abstractions;

namespace Usm.Shared.Algorithms.Collections.FenwickTree.Builders;

/// <summary>
/// Fluent builder for Fenwick tree configuration.
/// </summary>
/// <typeparam name="T">The numeric type.</typeparam>
public sealed class FenwickTreeBuilder<T> : IFenwickTreeBuilder<T>
    where T : INumber<T>
{
    private int _length = 16;

    /// <inheritdoc />
    public IFenwickTreeBuilder<T> WithLength(int length)
    {
        _length = length > 0 ? length : throw new ArgumentOutOfRangeException(nameof(length));
        return this;
    }

    /// <inheritdoc />
    public IFenwickTree<T> Build() => new FenwickTree<T>(_length);
}
