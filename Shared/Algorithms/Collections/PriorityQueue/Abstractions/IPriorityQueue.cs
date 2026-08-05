namespace Usm.Shared.Algorithms.Collections.PriorityQueue.Abstractions;

/// <summary>
/// Represents a reusable priority queue.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
/// <typeparam name="TPriority">The priority type.</typeparam>
public interface IPriorityQueue<TItem, TPriority>
    where TItem : notnull
    where TPriority : notnull
{
    /// <summary>Gets the number of enqueued items.</summary>
    int Count { get; }

    /// <summary>Enqueues an item with priority.</summary>
    void Enqueue(TItem item, TPriority priority);

    /// <summary>Enqueues an item asynchronously.</summary>
    ValueTask EnqueueAsync(TItem item, TPriority priority, CancellationToken cancellationToken = default);

    /// <summary>Dequeues the highest-priority item.</summary>
    TItem Dequeue();

    /// <summary>Attempts to dequeue the highest-priority item.</summary>
    bool TryDequeue(out TItem item, out TPriority priority);

    /// <summary>Peeks at the next item.</summary>
    TItem Peek();

    /// <summary>Attempts to peek at the next item.</summary>
    bool TryPeek(out TItem item, out TPriority priority);
}
