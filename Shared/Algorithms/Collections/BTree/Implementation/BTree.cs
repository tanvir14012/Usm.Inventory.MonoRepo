using Usm.Shared.Algorithms.Collections.BTree.Abstractions;

namespace Usm.Shared.Algorithms.Collections.BTree;

/// <summary>
/// Thread-safe generic B-tree with insert and search support.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public sealed class BTree<TKey, TValue> : IBTree<TKey, TValue>
    where TKey : notnull
{
    private readonly IComparer<TKey> _comparer;
    private readonly object _gate = new();
    private Node _root;

    /// <summary>Initializes a new B-tree.</summary>
    public BTree(BTreeOptions<TKey>? options = null)
    {
        _comparer = options?.Comparer ?? Comparer<TKey>.Default;
        MinimumDegree = options?.MinimumDegree >= 2 ? options.MinimumDegree : 16;
        Comparer = _comparer;
        _root = new Node(isLeaf: true);
    }

    /// <inheritdoc />
    public IComparer<TKey> Comparer { get; }

    /// <inheritdoc />
    public int MinimumDegree { get; }

    /// <inheritdoc />
    public int Count { get; private set; }

    /// <inheritdoc />
    public void Add(TKey key, TValue value)
    {
        lock (_gate)
        {
            if (_root.Keys.Count == MaxKeys)
            {
                var newRoot = new Node(isLeaf: false);
                newRoot.Children.Add(_root);
                SplitChild(newRoot, 0);
                _root = newRoot;
            }

            if (InsertNonFull(_root, key, value))
                Count++;
        }
    }

    /// <inheritdoc />
    public ValueTask AddAsync(TKey key, TValue value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Add(key, value);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => TryGetValue(key, out _);

    /// <inheritdoc />
    public ValueTask<bool> ContainsKeyAsync(TKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ContainsKey(key));
    }

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue value)
    {
        lock (_gate)
        {
            return TryGetValue(_root, key, out value);
        }
    }

    /// <inheritdoc />
    public ValueTask<(bool Found, TValue Value)> TryGetValueAsync(TKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(TryGetValue(key, out var value) ? (true, value) : (false, default!));
    }

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<TKey, TValue>> Traverse()
    {
        lock (_gate)
            return Traverse(_root).ToArray();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<KeyValuePair<TKey, TValue>> TraverseAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in Traverse())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
            await ValueTask.CompletedTask;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_gate)
        {
            _root = new Node(isLeaf: true);
            Count = 0;
        }
    }

    private int MaxKeys => 2 * MinimumDegree - 1;

    private bool InsertNonFull(Node node, TKey key, TValue value)
    {
        var index = FindKeyIndex(node.Keys, key);
        if (index < node.Keys.Count && _comparer.Compare(node.Keys[index], key) == 0)
        {
            node.Values[index] = value;
            return false;
        }

        if (node.IsLeaf)
        {
            node.Keys.Insert(index, key);
            node.Values.Insert(index, value);
            return true;
        }

        if (node.Children[index].Keys.Count == MaxKeys)
        {
            SplitChild(node, index);
            var comparison = _comparer.Compare(key, node.Keys[index]);
            if (comparison > 0)
                index++;
            else if (comparison == 0)
            {
                node.Values[index] = value;
                return false;
            }
        }

        return InsertNonFull(node.Children[index], key, value);
    }

    private void SplitChild(Node parent, int childIndex)
    {
        var fullChild = parent.Children[childIndex];
        var medianIndex = MinimumDegree - 1;
        var medianKey = fullChild.Keys[medianIndex];
        var medianValue = fullChild.Values[medianIndex];

        var rightChild = new Node(isLeaf: fullChild.IsLeaf);
        for (var i = medianIndex + 1; i < fullChild.Keys.Count; i++)
        {
            rightChild.Keys.Add(fullChild.Keys[i]);
            rightChild.Values.Add(fullChild.Values[i]);
        }

        if (!fullChild.IsLeaf)
        {
            for (var i = medianIndex + 1; i < fullChild.Children.Count; i++)
                rightChild.Children.Add(fullChild.Children[i]);
        }

        fullChild.Keys.RemoveRange(medianIndex, fullChild.Keys.Count - medianIndex);
        fullChild.Values.RemoveRange(medianIndex, fullChild.Values.Count - medianIndex);
        if (!fullChild.IsLeaf)
            fullChild.Children.RemoveRange(medianIndex + 1, fullChild.Children.Count - (medianIndex + 1));

        parent.Keys.Insert(childIndex, medianKey);
        parent.Values.Insert(childIndex, medianValue);
        parent.Children.Insert(childIndex + 1, rightChild);
    }

    private bool TryGetValue(Node node, TKey key, out TValue value)
    {
        var index = FindKeyIndex(node.Keys, key);
        if (index < node.Keys.Count && _comparer.Compare(node.Keys[index], key) == 0)
        {
            value = node.Values[index];
            return true;
        }

        if (node.IsLeaf)
        {
            value = default!;
            return false;
        }

        return TryGetValue(node.Children[index], key, out value);
    }

    private IEnumerable<KeyValuePair<TKey, TValue>> Traverse(Node node)
    {
        for (var i = 0; i < node.Keys.Count; i++)
        {
            if (!node.IsLeaf)
            {
                foreach (var item in Traverse(node.Children[i]))
                    yield return item;
            }

            yield return new KeyValuePair<TKey, TValue>(node.Keys[i], node.Values[i]);
        }

        if (!node.IsLeaf)
        {
            foreach (var item in Traverse(node.Children[node.Keys.Count]))
                yield return item;
        }
    }

    private int FindKeyIndex(List<TKey> keys, TKey key)
    {
        var low = 0;
        var high = keys.Count - 1;

        while (low <= high)
        {
            var mid = low + ((high - low) >> 1);
            var comparison = _comparer.Compare(keys[mid], key);
            if (comparison == 0)
                return mid;

            if (comparison < 0)
                low = mid + 1;
            else
                high = mid - 1;
        }

        return low;
    }

    private sealed class Node
    {
        public Node(bool isLeaf)
        {
            IsLeaf = isLeaf;
        }

        public bool IsLeaf { get; }

        public List<TKey> Keys { get; } = [];

        public List<TValue> Values { get; } = [];

        public List<Node> Children { get; } = [];
    }
}
