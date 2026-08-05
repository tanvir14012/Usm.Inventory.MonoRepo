namespace Usm.Shared.Algorithms.Collections.CircularBuffer.Abstractions;

/// <summary>
/// Represents a fixed-capacity circular buffer.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public interface ICircularBuffer<T>
{
    /// <summary>Gets the current item count.</summary>
    int Count { get; }

    /// <summary>Gets the buffer capacity.</summary>
    int Capacity { get; }

    /// <summary>Adds an item, evicting the oldest item when full.</summary>
    void Enqueue(T item);

    /// <summary>Adds an item asynchronously.</summary>
    ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default);

    /// <summary>Removes and returns the oldest item.</summary>
    T Dequeue();

    /// <summary>Attempts to remove and return the oldest item.</summary>
    bool TryDequeue(out T item);

    /// <summary>Peeks at the oldest item.</summary>
    T Peek();

    /// <summary>Attempts to peek at the oldest item.</summary>
    bool TryPeek(out T item);

    /// <summary>Clears the buffer.</summary>
    void Clear();

    /// <summary>Returns a snapshot of the buffer contents in queue order.</summary>
    IReadOnlyList<T> Snapshot();
}
