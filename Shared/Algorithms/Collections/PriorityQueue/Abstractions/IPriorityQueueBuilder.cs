namespace Usm.Shared.Algorithms.Collections.PriorityQueue.Abstractions;

/// <summary>
/// Fluent builder for priority queue configuration.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
/// <typeparam name="TPriority">The priority type.</typeparam>
public interface IPriorityQueueBuilder<TItem, TPriority>
    where TItem : notnull
    where TPriority : notnull
{
    /// <summary>Sets the comparer used for priorities.</summary>
    IPriorityQueueBuilder<TItem, TPriority> WithPriorityComparer(IComparer<TPriority> comparer);

    /// <summary>Sets whether older items with equal priority are dequeued first.</summary>
    IPriorityQueueBuilder<TItem, TPriority> WithStableOrdering(bool enabled);

    /// <summary>Builds the queue.</summary>
    IPriorityQueue<TItem, TPriority> Build();
}
