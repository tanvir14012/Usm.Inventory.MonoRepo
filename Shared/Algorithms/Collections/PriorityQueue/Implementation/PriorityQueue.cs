using Usm.Shared.Algorithms.Collections.PriorityQueue.Abstractions;

namespace Usm.Shared.Algorithms.Collections.PriorityQueue;

/// <summary>
/// Binary-heap priority queue with optional stable ordering.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
/// <typeparam name="TPriority">The priority type.</typeparam>
public sealed class PriorityQueue<TItem, TPriority> : IPriorityQueue<TItem, TPriority>
    where TItem : notnull
    where TPriority : notnull
{
    private readonly List<Entry> _heap = new();
    private readonly IComparer<TPriority> _comparer;
    private readonly bool _stableOrdering;
    private long _sequence;

    /// <summary>Initializes a new queue.</summary>
    public PriorityQueue(IComparer<TPriority> comparer, bool stableOrdering = true)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        _stableOrdering = stableOrdering;
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_heap)
                return _heap.Count;
        }
    }

    /// <inheritdoc />
    public void Enqueue(TItem item, TPriority priority)
    {
        lock (_heap)
        {
            Add(new Entry(item, priority, _sequence++));
        }
    }

    /// <inheritdoc />
    public ValueTask EnqueueAsync(TItem item, TPriority priority, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Enqueue(item, priority);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public TItem Dequeue()
    {
        lock (_heap)
        {
            if (!TryRemoveRoot(out var entry))
                throw new InvalidOperationException("The queue is empty.");

            return entry.Item;
        }
    }

    /// <inheritdoc />
    public bool TryDequeue(out TItem item, out TPriority priority)
    {
        lock (_heap)
        {
            if (TryRemoveRoot(out var entry))
            {
                item = entry.Item;
                priority = entry.Priority;
                return true;
            }
        }

        item = default!;
        priority = default!;
        return false;
    }

    /// <inheritdoc />
    public TItem Peek()
    {
        lock (_heap)
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("The queue is empty.");

            return _heap[0].Item;
        }
    }

    /// <inheritdoc />
    public bool TryPeek(out TItem item, out TPriority priority)
    {
        lock (_heap)
        {
            if (_heap.Count > 0)
            {
                item = _heap[0].Item;
                priority = _heap[0].Priority;
                return true;
            }
        }

        item = default!;
        priority = default!;
        return false;
    }

    private void Add(Entry entry)
    {
        _heap.Add(entry);
        SiftUp(_heap.Count - 1);
    }

    private bool TryRemoveRoot(out Entry entry)
    {
        if (_heap.Count == 0)
        {
            entry = default;
            return false;
        }

        entry = _heap[0];
        var last = _heap.Count - 1;
        _heap[0] = _heap[last];
        _heap.RemoveAt(last);

        if (_heap.Count > 0)
            SiftDown(0);

        return true;
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            var parent = (index - 1) / 2;
            if (Compare(_heap[index], _heap[parent]) >= 0)
                break;

            (_heap[index], _heap[parent]) = (_heap[parent], _heap[index]);
            index = parent;
        }
    }

    private void SiftDown(int index)
    {
        while (true)
        {
            var left = index * 2 + 1;
            var right = left + 1;
            var smallest = index;

            if (left < _heap.Count && Compare(_heap[left], _heap[smallest]) < 0)
                smallest = left;

            if (right < _heap.Count && Compare(_heap[right], _heap[smallest]) < 0)
                smallest = right;

            if (smallest == index)
                return;

            (_heap[index], _heap[smallest]) = (_heap[smallest], _heap[index]);
            index = smallest;
        }
    }

    private int Compare(Entry x, Entry y)
    {
        var byPriority = _comparer.Compare(x.Priority, y.Priority);
        if (byPriority != 0 || !_stableOrdering)
            return byPriority;

        return x.Sequence.CompareTo(y.Sequence);
    }

    private readonly record struct Entry(TItem Item, TPriority Priority, long Sequence);
}
