using System.Numerics;
using Usm.Shared.Algorithms.Collections.KdTree.Abstractions;

namespace Usm.Shared.Algorithms.Collections.KdTree;

/// <summary>
/// AVL-free KD-tree for 2D nearest-neighbor and range queries.
/// </summary>
/// <typeparam name="TCoordinate">The coordinate type.</typeparam>
/// <typeparam name="TValue">The payload type.</typeparam>
public sealed class KdTree<TCoordinate, TValue> : IKdTree<TCoordinate, TValue>
    where TCoordinate : IFloatingPointIeee754<TCoordinate>
{
    private readonly object _gate = new();
    private readonly bool _allowDuplicatePoints;
    private Node? _root;

    /// <summary>Initializes a new KD-tree.</summary>
    public KdTree(KdTreeOptions<TCoordinate>? options = null)
    {
        _allowDuplicatePoints = options?.AllowDuplicatePoints ?? true;
    }

    /// <inheritdoc />
    public int Count { get; private set; }

    /// <inheritdoc />
    public void Add(TCoordinate x, TCoordinate y, TValue value)
    {
        lock (_gate)
        {
            var point = new KdPoint<TCoordinate, TValue>(x, y, value);
            _root = Insert(_root, point, 0, out var inserted);
            if (inserted)
                Count++;
        }
    }

    /// <inheritdoc />
    public ValueTask AddAsync(TCoordinate x, TCoordinate y, TValue value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Add(x, y, value);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public bool Remove(TCoordinate x, TCoordinate y, TValue value)
    {
        lock (_gate)
        {
            var removed = false;
            _root = Remove(_root, new KdPoint<TCoordinate, TValue>(x, y, value), 0, out removed);
            if (removed)
                Count--;

            return removed;
        }
    }

    /// <inheritdoc />
    public ValueTask<bool> RemoveAsync(TCoordinate x, TCoordinate y, TValue value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Remove(x, y, value));
    }

    /// <inheritdoc />
    public IReadOnlyList<KdPoint<TCoordinate, TValue>> QueryRange(TCoordinate minX, TCoordinate minY, TCoordinate maxX, TCoordinate maxY)
    {
        if (minX > maxX)
            throw new ArgumentOutOfRangeException(nameof(maxX));
        if (minY > maxY)
            throw new ArgumentOutOfRangeException(nameof(maxY));

        lock (_gate)
        {
            var results = new List<KdPoint<TCoordinate, TValue>>();
            QueryRange(_root, minX, minY, maxX, maxY, 0, results);
            return results;
        }
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<KdPoint<TCoordinate, TValue>>> QueryRangeAsync(TCoordinate minX, TCoordinate minY, TCoordinate maxX, TCoordinate maxY, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(QueryRange(minX, minY, maxX, maxY));
    }

    /// <inheritdoc />
    public KdPoint<TCoordinate, TValue>? NearestNeighbor(TCoordinate x, TCoordinate y)
    {
        lock (_gate)
        {
            KdPoint<TCoordinate, TValue>? best = null;
            var bestDistance = TCoordinate.PositiveInfinity;
            SearchNearest(_root, x, y, 0, ref best, ref bestDistance);
            return best;
        }
    }

    /// <inheritdoc />
    public ValueTask<KdPoint<TCoordinate, TValue>?> NearestNeighborAsync(TCoordinate x, TCoordinate y, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(NearestNeighbor(x, y));
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

    private Node Insert(Node? node, KdPoint<TCoordinate, TValue> point, int depth, out bool inserted)
    {
        if (node is null)
        {
            inserted = true;
            return new Node(point);
        }

        if (!_allowDuplicatePoints && PointsEqual(node.Point, point))
        {
            node.Point = point;
            inserted = false;
            return node;
        }

        var axis = depth & 1;
        if (CompareOnAxis(point, node.Point, axis) < 0)
            node.Left = Insert(node.Left, point, depth + 1, out inserted);
        else
            node.Right = Insert(node.Right, point, depth + 1, out inserted);

        return node;
    }

    private Node? Remove(Node? node, KdPoint<TCoordinate, TValue> point, int depth, out bool removed)
    {
        if (node is null)
        {
            removed = false;
            return null;
        }

        if (PointsEqual(node.Point, point))
        {
            removed = true;
            if (node.Right is not null)
            {
                var replacement = FindMin(node.Right, depth & 1, depth + 1);
                node.Point = replacement.Point;
                node.Right = Remove(node.Right, replacement.Point, depth + 1, out _);
                return node;
            }

            if (node.Left is not null)
            {
                var replacement = FindMin(node.Left, depth & 1, depth + 1);
                node.Point = replacement.Point;
                node.Right = Remove(node.Left, replacement.Point, depth + 1, out _);
                node.Left = null;
                return node;
            }

            return null;
        }

        var axis = depth & 1;
        if (CompareOnAxis(point, node.Point, axis) < 0)
            node.Left = Remove(node.Left, point, depth + 1, out removed);
        else
            node.Right = Remove(node.Right, point, depth + 1, out removed);

        return node;
    }

    private Node FindMin(Node node, int axis, int depth)
    {
        var currentAxis = depth & 1;
        if (currentAxis == axis)
        {
            if (node.Left is null)
                return node;

            return FindMin(node.Left, axis, depth + 1);
        }

        var best = node;
        if (node.Left is not null)
        {
            var candidate = FindMin(node.Left, axis, depth + 1);
            if (CompareOnAxis(candidate.Point, best.Point, axis) < 0)
                best = candidate;
        }

        if (node.Right is not null)
        {
            var candidate = FindMin(node.Right, axis, depth + 1);
            if (CompareOnAxis(candidate.Point, best.Point, axis) < 0)
                best = candidate;
        }

        return best;
    }

    private void QueryRange(Node? node, TCoordinate minX, TCoordinate minY, TCoordinate maxX, TCoordinate maxY, int depth, List<KdPoint<TCoordinate, TValue>> results)
    {
        if (node is null)
            return;

        if (node.Point.X >= minX && node.Point.X <= maxX && node.Point.Y >= minY && node.Point.Y <= maxY)
            results.Add(node.Point);

        var axis = depth & 1;
        if (axis == 0)
        {
            if (minX <= node.Point.X)
                QueryRange(node.Left, minX, minY, maxX, maxY, depth + 1, results);
            if (node.Point.X <= maxX)
                QueryRange(node.Right, minX, minY, maxX, maxY, depth + 1, results);
            return;
        }

        if (minY <= node.Point.Y)
            QueryRange(node.Left, minX, minY, maxX, maxY, depth + 1, results);
        if (node.Point.Y <= maxY)
            QueryRange(node.Right, minX, minY, maxX, maxY, depth + 1, results);
    }

    private void SearchNearest(Node? node, TCoordinate x, TCoordinate y, int depth, ref KdPoint<TCoordinate, TValue>? best, ref TCoordinate bestDistance)
    {
        if (node is null)
            return;

        var currentDistance = DistanceSquared(node.Point.X, node.Point.Y, x, y);
        if (best is null || currentDistance < bestDistance)
        {
            best = node.Point;
            bestDistance = currentDistance;
        }

        var axis = depth & 1;
        Node? near;
        Node? far;
        var targetAxis = axis == 0 ? x : y;
        var nodeAxis = axis == 0 ? node.Point.X : node.Point.Y;

        if (targetAxis < nodeAxis)
        {
            near = node.Left;
            far = node.Right;
        }
        else
        {
            near = node.Right;
            far = node.Left;
        }

        SearchNearest(near, x, y, depth + 1, ref best, ref bestDistance);

        var axisDistance = DifferenceSquared(targetAxis, nodeAxis);
        if (best is null || axisDistance <= bestDistance)
            SearchNearest(far, x, y, depth + 1, ref best, ref bestDistance);
    }

    private static TCoordinate DifferenceSquared(TCoordinate left, TCoordinate right)
    {
        var diff = left - right;
        return diff * diff;
    }

    private static TCoordinate DistanceSquared(TCoordinate x1, TCoordinate y1, TCoordinate x2, TCoordinate y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return dx * dx + dy * dy;
    }

    private static int CompareOnAxis(KdPoint<TCoordinate, TValue> left, KdPoint<TCoordinate, TValue> right, int axis)
        => axis == 0 ? left.X.CompareTo(right.X) : left.Y.CompareTo(right.Y);

    private static bool PointsEqual(KdPoint<TCoordinate, TValue> left, KdPoint<TCoordinate, TValue> right)
        => left.X == right.X && left.Y == right.Y && EqualityComparer<TValue>.Default.Equals(left.Value, right.Value);

    private sealed class Node
    {
        public Node(KdPoint<TCoordinate, TValue> point)
        {
            Point = point;
        }

        public KdPoint<TCoordinate, TValue> Point { get; set; }

        public Node? Left { get; set; }

        public Node? Right { get; set; }
    }
}
