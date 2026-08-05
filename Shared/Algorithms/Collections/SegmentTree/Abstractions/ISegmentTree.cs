using System.Numerics;

namespace Usm.Shared.Algorithms.Collections.SegmentTree.Abstractions;

/// <summary>
/// Represents a reusable segment tree.
/// </summary>
/// <typeparam name="T">The numeric type.</typeparam>
public interface ISegmentTree<T>
    where T : INumber<T>
{
    /// <summary>Gets the logical length.</summary>
    int Length { get; }

    /// <summary>Updates a value at an index by a delta.</summary>
    void Add(int index, T delta);

    /// <summary>Updates a value asynchronously.</summary>
    ValueTask AddAsync(int index, T delta, CancellationToken cancellationToken = default);

    /// <summary>Queries an inclusive range.</summary>
    T Query(int left, int right);

    /// <summary>Queries an inclusive range asynchronously.</summary>
    ValueTask<T> QueryAsync(int left, int right, CancellationToken cancellationToken = default);

    /// <summary>Clears the tree.</summary>
    void Clear();
}
