using Usm.Shared.Algorithms.Collections.PriorityQueue.Abstractions;

namespace Usm.Shared.Algorithms.Collections.PriorityQueue.Builders;

/// <summary>
/// Fluent builder for priority queue configuration.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
/// <typeparam name="TPriority">The priority type.</typeparam>
public sealed class PriorityQueueBuilder<TItem, TPriority> : IPriorityQueueBuilder<TItem, TPriority>
    where TItem : notnull
    where TPriority : notnull
{
    private IComparer<TPriority>? _comparer;
    private bool _stableOrdering = true;

    /// <inheritdoc />
    public IPriorityQueueBuilder<TItem, TPriority> WithPriorityComparer(IComparer<TPriority> comparer)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        return this;
    }

    /// <inheritdoc />
    public IPriorityQueueBuilder<TItem, TPriority> WithStableOrdering(bool enabled)
    {
        _stableOrdering = enabled;
        return this;
    }

    /// <inheritdoc />
    public IPriorityQueue<TItem, TPriority> Build()
        => new PriorityQueue<TItem, TPriority>(_comparer ?? Comparer<TPriority>.Default, _stableOrdering);
}
