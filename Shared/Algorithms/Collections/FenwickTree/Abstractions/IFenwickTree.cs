using System.Numerics;

namespace Usm.Shared.Algorithms.Collections.FenwickTree.Abstractions;

/// <summary>
/// Represents a reusable Fenwick tree / binary indexed tree.
/// </summary>
/// <typeparam name="T">The numeric type.</typeparam>
public interface IFenwickTree<T>
    where T : INumber<T>
{
    /// <summary>Gets the logical length of the tree.</summary>
    int Length { get; }

    /// <summary>Updates a value at the specified zero-based index by the supplied delta.</summary>
    void Add(int index, T delta);

    /// <summary>Updates a value asynchronously.</summary>
    ValueTask AddAsync(int index, T delta, CancellationToken cancellationToken = default);

    /// <summary>Returns the prefix sum from index 0 through the supplied index.</summary>
    T PrefixSum(int index);

    /// <summary>Returns the prefix sum asynchronously.</summary>
    ValueTask<T> PrefixSumAsync(int index, CancellationToken cancellationToken = default);

    /// <summary>Returns the sum across an inclusive range.</summary>
    T RangeSum(int left, int right);

    /// <summary>Returns the sum across an inclusive range asynchronously.</summary>
    ValueTask<T> RangeSumAsync(int left, int right, CancellationToken cancellationToken = default);

    /// <summary>Clears the tree.</summary>
    void Clear();
}
