using System.Numerics;
using Usm.Shared.Algorithms.Collections.SegmentTree.Abstractions;

namespace Usm.Shared.Algorithms.Collections.SegmentTree;

/// <summary>
/// Generic segment tree for range sums.
/// </summary>
/// <typeparam name="T">The numeric type.</typeparam>
public sealed class SegmentTree<T> : ISegmentTree<T>
    where T : INumber<T>
{
    private readonly T[] _tree;
    private readonly int _leafOffset;
    private readonly object _gate = new();

    /// <summary>Initializes a new segment tree.</summary>
    public SegmentTree(int length)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        Length = length;
        _leafOffset = 1;
        while (_leafOffset < length)
            _leafOffset <<= 1;

        _tree = new T[_leafOffset * 2];
    }

    /// <inheritdoc />
    public int Length { get; }

    /// <inheritdoc />
    public void Add(int index, T delta)
    {
        ValidateIndex(index);

        lock (_gate)
        {
            var node = _leafOffset + index;
            _tree[node] += delta;
            for (node >>= 1; node > 0; node >>= 1)
                _tree[node] = _tree[node * 2] + _tree[node * 2 + 1];
        }
    }

    /// <inheritdoc />
    public ValueTask AddAsync(int index, T delta, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Add(index, delta);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public T Query(int left, int right)
    {
        if (right < left)
            return T.Zero;

        ValidateIndex(left);
        ValidateIndex(right);

        lock (_gate)
        {
            var result = T.Zero;
            var l = _leafOffset + left;
            var r = _leafOffset + right;

            while (l <= r)
            {
                if ((l & 1) == 1)
                    result += _tree[l++];

                if ((r & 1) == 0)
                    result += _tree[r--];

                l >>= 1;
                r >>= 1;
            }

            return result;
        }
    }

    /// <inheritdoc />
    public ValueTask<T> QueryAsync(int left, int right, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Query(left, right));
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_gate)
            Array.Clear(_tree);
    }

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(index));
    }
}
