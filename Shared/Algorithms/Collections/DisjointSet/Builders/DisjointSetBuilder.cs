using Usm.Shared.Algorithms.Collections.DisjointSet.Abstractions;

namespace Usm.Shared.Algorithms.Collections.DisjointSet.Builders;

/// <summary>
/// Fluent builder for a disjoint set.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class DisjointSetBuilder<T>
    where T : notnull
{
    private IEqualityComparer<T>? _comparer;

    /// <summary>Uses a custom comparer.</summary>
    public DisjointSetBuilder<T> WithComparer(IEqualityComparer<T> comparer)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        return this;
    }

    /// <summary>Builds the disjoint set.</summary>
    public IDisjointSet<T> Build() => new DisjointSet<T>(_comparer ?? EqualityComparer<T>.Default);
}
