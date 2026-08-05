using System.Numerics;
using Usm.Shared.Algorithms.Collections.FenwickTree.Abstractions;

namespace Usm.Shared.Algorithms.Collections.FenwickTree;

/// <summary>
/// Generic Fenwick tree with prefix and range sums.
/// </summary>
/// <typeparam name="T">The numeric type.</typeparam>
public sealed class FenwickTree<T> : IFenwickTree<T>
    where T : INumber<T>
{
    private readonly T[] _tree;

    /// <summary>Initializes a new tree.</summary>
    public FenwickTree(int length)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        Length = length;
        _tree = new T[length + 1];
    }

    /// <inheritdoc />
    public int Length { get; }

    /// <inheritdoc />
    public void Add(int index, T delta)
    {
        ValidateIndex(index);

        for (var i = index + 1; i <= Length; i += i & -i)
            _tree[i] += delta;
    }

    /// <inheritdoc />
    public ValueTask AddAsync(int index, T delta, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Add(index, delta);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public T PrefixSum(int index)
    {
        if (index < 0)
            return T.Zero;

        if (index >= Length)
            index = Length - 1;

        var sum = T.Zero;
        for (var i = index + 1; i > 0; i -= i & -i)
            sum += _tree[i];

        return sum;
    }

    /// <inheritdoc />
    public ValueTask<T> PrefixSumAsync(int index, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(PrefixSum(index));
    }

    /// <inheritdoc />
    public T RangeSum(int left, int right)
    {
        if (right < left)
            return T.Zero;

        return PrefixSum(right) - PrefixSum(left - 1);
    }

    /// <inheritdoc />
    public ValueTask<T> RangeSumAsync(int left, int right, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(RangeSum(left, right));
    }

    /// <inheritdoc />
    public void Clear() => Array.Clear(_tree);

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(index));
    }
}
