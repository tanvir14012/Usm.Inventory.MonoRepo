using System.Collections.Immutable;
using Usm.Shared.Algorithms.Collections.DisjointSet.Abstractions;

namespace Usm.Shared.Algorithms.Collections.DisjointSet;

/// <summary>
/// Union-find with path compression and union by rank.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class DisjointSet<T> : IDisjointSet<T>
    where T : notnull
{
    private readonly Dictionary<T, Node> _nodes;
    private readonly object _gate = new();

    /// <summary>Initializes a new disjoint set.</summary>
    public DisjointSet(IEqualityComparer<T> comparer)
    {
        _nodes = new Dictionary<T, Node>(comparer ?? EqualityComparer<T>.Default);
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_gate)
                return _nodes.Count;
        }
    }

    /// <inheritdoc />
    public bool Add(T item)
    {
        lock (_gate)
        {
            if (_nodes.ContainsKey(item))
                return false;

            _nodes[item] = new Node(item);
            return true;
        }
    }

    /// <inheritdoc />
    public T Find(T item)
    {
        lock (_gate)
        {
            var node = GetNode(item);
            return FindRoot(node).Value;
        }
    }

    /// <inheritdoc />
    public T Union(T first, T second)
    {
        lock (_gate)
        {
            var rootA = FindRoot(GetNode(first));
            var rootB = FindRoot(GetNode(second));
            if (ReferenceEquals(rootA, rootB))
                return rootA.Value;

            if (rootA.Rank < rootB.Rank)
            {
                rootA.Parent = rootB;
                rootB.Size += rootA.Size;
                return rootB.Value;
            }

            if (rootA.Rank > rootB.Rank)
            {
                rootB.Parent = rootA;
                rootA.Size += rootB.Size;
                return rootA.Value;
            }

            rootB.Parent = rootA;
            rootA.Rank++;
            rootA.Size += rootB.Size;
            return rootA.Value;
        }
    }

    /// <inheritdoc />
    public bool Connected(T first, T second)
    {
        lock (_gate)
            return ReferenceEquals(FindRoot(GetNode(first)), FindRoot(GetNode(second)));
    }

    /// <inheritdoc />
    public int SetSize(T item)
    {
        lock (_gate)
            return FindRoot(GetNode(item)).Size;
    }

    private Node GetNode(T item)
    {
        if (!_nodes.TryGetValue(item, out var node))
        {
            node = new Node(item);
            _nodes[item] = node;
        }

        return node;
    }

    private static Node FindRoot(Node node)
    {
        if (!ReferenceEquals(node.Parent, node))
            node.Parent = FindRoot(node.Parent);

        return node.Parent;
    }

    private sealed class Node
    {
        public Node(T value)
        {
            Value = value;
            Parent = this;
            Size = 1;
        }

        public T Value { get; }
        public Node Parent { get; set; }
        public int Rank { get; set; }
        public int Size { get; set; }
    }
}
