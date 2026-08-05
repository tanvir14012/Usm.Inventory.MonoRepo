using Usm.Shared.Algorithms.Collections.CircularBuffer.Abstractions;

namespace Usm.Shared.Algorithms.Collections.CircularBuffer;

/// <summary>
/// Thread-safe circular buffer with overwrite-on-full semantics.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class CircularBuffer<T> : ICircularBuffer<T>
{
    private readonly T[] _items;
    private readonly object _gate = new();
    private int _head;
    private int _tail;
    private int _count;

    /// <summary>Initializes a new circular buffer.</summary>
    public CircularBuffer(int capacity)
    {
        _items = capacity > 0 ? new T[capacity] : throw new ArgumentOutOfRangeException(nameof(capacity));
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_gate)
                return _count;
        }
    }

    /// <inheritdoc />
    public int Capacity => _items.Length;

    /// <inheritdoc />
    public void Enqueue(T item)
    {
        lock (_gate)
        {
            _items[_tail] = item;
            _tail = (_tail + 1) % _items.Length;
            if (_count == _items.Length)
            {
                _head = (_head + 1) % _items.Length;
            }
            else
            {
                _count++;
            }
        }
    }

    /// <inheritdoc />
    public ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Enqueue(item);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public T Dequeue()
    {
        lock (_gate)
        {
            if (_count == 0)
                throw new InvalidOperationException("The buffer is empty.");

            var item = _items[_head];
            _items[_head] = default!;
            _head = (_head + 1) % _items.Length;
            _count--;
            return item;
        }
    }

    /// <inheritdoc />
    public bool TryDequeue(out T item)
    {
        lock (_gate)
        {
            if (_count == 0)
            {
                item = default!;
                return false;
            }

            item = Dequeue();
            return true;
        }
    }

    /// <inheritdoc />
    public T Peek()
    {
        lock (_gate)
        {
            if (_count == 0)
                throw new InvalidOperationException("The buffer is empty.");

            return _items[_head];
        }
    }

    /// <inheritdoc />
    public bool TryPeek(out T item)
    {
        lock (_gate)
        {
            if (_count == 0)
            {
                item = default!;
                return false;
            }

            item = _items[_head];
            return true;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_gate)
        {
            Array.Clear(_items);
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<T> Snapshot()
    {
        lock (_gate)
        {
            var snapshot = new T[_count];
            for (var i = 0; i < _count; i++)
                snapshot[i] = _items[(_head + i) % _items.Length];

            return snapshot;
        }
    }
}
