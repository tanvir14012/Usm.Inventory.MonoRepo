using System.Collections.Generic;
using Usm.Shared.Algorithms.Collections.IntervalTree.Abstractions;

namespace Usm.Shared.Algorithms.Collections.IntervalTree;

/// <summary>
/// AVL-based interval tree for overlap queries.
/// </summary>
/// <typeparam name="TKey">The interval boundary type.</typeparam>
/// <typeparam name="TValue">The stored payload type.</typeparam>
public sealed class IntervalTree<TKey, TValue> : IIntervalTree<TKey, TValue>
    where TKey : notnull
{
    private readonly IComparer<TKey> _comparer;
    private readonly object _gate = new();
    private Node? _root;

    /// <summary>Initializes a new interval tree.</summary>
    public IntervalTree(IntervalTreeOptions<TKey>? options = null)
    {
        _comparer = options?.Comparer ?? Comparer<TKey>.Default;
        Comparer = _comparer;
    }

    /// <inheritdoc />
    public IComparer<TKey> Comparer { get; }

    /// <inheritdoc />
    public int Count { get; private set; }

    /// <inheritdoc />
    public void Add(TKey start, TKey end, TValue value)
    {
        ValidateInterval(start, end);

        lock (_gate)
        {
            _root = Insert(_root, new Interval<TKey, TValue>(start, end, value));
            Count++;
        }
    }

    /// <inheritdoc />
    public ValueTask AddAsync(TKey start, TKey end, TValue value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Add(start, end, value);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public bool Remove(TKey start, TKey end, TValue value)
    {
        ValidateInterval(start, end);

        lock (_gate)
        {
            var removed = false;
            _root = Remove(_root, new Interval<TKey, TValue>(start, end, value), out removed);
            if (removed)
                Count--;

            return removed;
        }
    }

    /// <inheritdoc />
    public ValueTask<bool> RemoveAsync(TKey start, TKey end, TValue value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Remove(start, end, value));
    }

    /// <inheritdoc />
    public IReadOnlyList<Interval<TKey, TValue>> QueryOverlapping(TKey start, TKey end)
    {
        ValidateInterval(start, end);

        lock (_gate)
        {
            var results = new List<Interval<TKey, TValue>>();
            QueryOverlapping(_root, start, end, results);
            return results;
        }
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<Interval<TKey, TValue>>> QueryOverlappingAsync(TKey start, TKey end, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(QueryOverlapping(start, end));
    }

    /// <inheritdoc />
    public IReadOnlyList<Interval<TKey, TValue>> QueryPoint(TKey point) => QueryOverlapping(point, point);

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<Interval<TKey, TValue>>> QueryPointAsync(TKey point, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(QueryPoint(point));
    }

    /// <inheritdoc />
    public bool ContainsOverlap(TKey start, TKey end)
    {
        ValidateInterval(start, end);

        lock (_gate)
            return ContainsOverlap(_root, start, end);
    }

    /// <inheritdoc />
    public ValueTask<bool> ContainsOverlapAsync(TKey start, TKey end, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ContainsOverlap(start, end));
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_gate)
        {
            _root = null;
            Count = 0;
        }
    }

    private static void ValidateInterval(TKey start, TKey end)
    {
        if (Comparer<TKey>.Default.Compare(start, end) > 0)
            throw new ArgumentOutOfRangeException(nameof(end));
    }

    private Node Insert(Node? node, Interval<TKey, TValue> interval)
    {
        if (node is null)
            return new Node(interval);

        var cmp = Compare(interval.Start, node.Interval.Start);
        if (cmp < 0 || (cmp == 0 && Compare(interval.End, node.Interval.End) < 0))
        {
            node.Left = Insert(node.Left, interval);
        }
        else
        {
            node.Right = Insert(node.Right, interval);
        }

        Update(node);
        return Balance(node);
    }

    private Node? Remove(Node? node, Interval<TKey, TValue> interval, out bool removed)
    {
        if (node is null)
        {
            removed = false;
            return null;
        }

        var cmp = Compare(interval.Start, node.Interval.Start);
        if (cmp < 0 || (cmp == 0 && Compare(interval.End, node.Interval.End) < 0))
        {
            node.Left = Remove(node.Left, interval, out removed);
            if (removed)
            {
                Update(node);
                return Balance(node);
            }

            return node;
        }

        if (cmp > 0 || (cmp == 0 && Compare(interval.End, node.Interval.End) > 0))
        {
            node.Right = Remove(node.Right, interval, out removed);
            if (removed)
            {
                Update(node);
                return Balance(node);
            }

            return node;
        }

        if (!EqualityComparer<TValue>.Default.Equals(node.Interval.Value, interval.Value))
        {
            node.Right = Remove(node.Right, interval, out removed);
            if (removed)
            {
                Update(node);
                return Balance(node);
            }

            return node;
        }

        removed = true;
        return RemoveNode(node);
    }

    private Node? RemoveNode(Node node)
    {
        if (node.Left is null)
            return node.Right;

        if (node.Right is null)
            return node.Left;

        var successor = GetMin(node.Right);
        node.Interval = successor.Interval;
        node.Right = Remove(node.Right, successor.Interval, out _);
        Update(node);
        return Balance(node);
    }

    private Node GetMin(Node node)
    {
        while (node.Left is not null)
            node = node.Left;

        return node;
    }

    private void QueryOverlapping(Node? node, TKey start, TKey end, List<Interval<TKey, TValue>> results)
    {
        if (node is null)
            return;

        if (node.Left is not null && Compare(node.Left.MaxEnd, start) >= 0)
            QueryOverlapping(node.Left, start, end, results);

        if (Overlaps(node.Interval, start, end))
            results.Add(node.Interval);

        if (node.Right is not null && Compare(node.Interval.Start, end) <= 0)
            QueryOverlapping(node.Right, start, end, results);
    }

    private bool ContainsOverlap(Node? node, TKey start, TKey end)
    {
        if (node is null)
            return false;

        if (node.Left is not null && Compare(node.Left.MaxEnd, start) >= 0 && ContainsOverlap(node.Left, start, end))
            return true;

        if (Overlaps(node.Interval, start, end))
            return true;

        return node.Right is not null && Compare(node.Interval.Start, end) <= 0 && ContainsOverlap(node.Right, start, end);
    }

    private bool Overlaps(Interval<TKey, TValue> interval, TKey start, TKey end)
        => Compare(interval.Start, end) <= 0 && Compare(start, interval.End) <= 0;

    private int Compare(TKey left, TKey right) => _comparer.Compare(left, right);

    private static int Height(Node? node) => node?.Height ?? 0;

    private static TKey MaxOf(TKey first, TKey second, IComparer<TKey> comparer)
        => comparer.Compare(first, second) >= 0 ? first : second;

    private void Update(Node node)
    {
        node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
        var maxEnd = node.Interval.End;
        if (node.Left is not null)
            maxEnd = MaxOf(maxEnd, node.Left.MaxEnd, _comparer);
        if (node.Right is not null)
            maxEnd = MaxOf(maxEnd, node.Right.MaxEnd, _comparer);
        node.MaxEnd = maxEnd;
    }

    private Node Balance(Node node)
    {
        var balance = Height(node.Left) - Height(node.Right);
        if (balance > 1)
        {
            if (node.Left is not null && Height(node.Left.Left) < Height(node.Left.Right))
                node.Left = RotateLeft(node.Left);

            return RotateRight(node);
        }

        if (balance < -1)
        {
            if (node.Right is not null && Height(node.Right.Right) < Height(node.Right.Left))
                node.Right = RotateRight(node.Right);

            return RotateLeft(node);
        }

        return node;
    }

    private Node RotateLeft(Node node)
    {
        var right = node.Right ?? throw new InvalidOperationException("Right child required.");
        node.Right = right.Left;
        right.Left = node;
        Update(node);
        Update(right);
        return right;
    }

    private Node RotateRight(Node node)
    {
        var left = node.Left ?? throw new InvalidOperationException("Left child required.");
        node.Left = left.Right;
        left.Right = node;
        Update(node);
        Update(left);
        return left;
    }

    private sealed class Node
    {
        public Node(Interval<TKey, TValue> interval)
        {
            Interval = interval;
            MaxEnd = interval.End;
            Height = 1;
        }

        public Interval<TKey, TValue> Interval { get; set; }

        public TKey MaxEnd { get; set; }

        public int Height { get; set; }

        public Node? Left { get; set; }

        public Node? Right { get; set; }
    }
}
