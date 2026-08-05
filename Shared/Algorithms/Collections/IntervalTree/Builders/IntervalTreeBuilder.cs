using Usm.Shared.Algorithms.Collections.IntervalTree;
using Usm.Shared.Algorithms.Collections.IntervalTree.Abstractions;

namespace Usm.Shared.Algorithms.Collections.IntervalTree.Builders;

/// <summary>
/// Fluent builder for interval trees.
/// </summary>
/// <typeparam name="TKey">The interval boundary type.</typeparam>
/// <typeparam name="TValue">The stored payload type.</typeparam>
public sealed class IntervalTreeBuilder<TKey, TValue> : IIntervalTreeBuilder<TKey, TValue>
    where TKey : notnull
{
    private readonly IntervalTreeOptions<TKey> _options = new();

    /// <inheritdoc />
    public IIntervalTreeBuilder<TKey, TValue> WithComparer(IComparer<TKey> comparer)
    {
        _options.Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        return this;
    }

    /// <inheritdoc />
    public IIntervalTree<TKey, TValue> Build() => new IntervalTree<TKey, TValue>(_options);
}
