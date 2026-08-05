using System.Numerics;

namespace Usm.Shared.Algorithms.Collections.FenwickTree.Abstractions;

/// <summary>
/// Fluent builder for Fenwick tree configuration.
/// </summary>
/// <typeparam name="T">The numeric type.</typeparam>
public interface IFenwickTreeBuilder<T>
    where T : INumber<T>
{
    /// <summary>Sets the tree length.</summary>
    IFenwickTreeBuilder<T> WithLength(int length);

    /// <summary>Builds the tree.</summary>
    IFenwickTree<T> Build();
}
