using Usm.Shared.Algorithms.Collections.Trie.Abstractions;

namespace Usm.Shared.Algorithms.Collections.Trie;

/// <summary>
/// Thread-safe trie with prefix enumeration.
/// </summary>
/// <typeparam name="TValue">The stored value type.</typeparam>
public sealed class Trie<TValue> : ITrie<TValue>
{
    private readonly Node _root = new();
    private readonly StringComparer _comparer;
    private readonly object _gate = new();
    private int _count;

    /// <summary>Initializes a new trie.</summary>
    public Trie(StringComparer comparer)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
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
    public void Add(string key, TValue value, bool overwrite = true)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        lock (_gate)
        {
            var node = GetOrCreateNode(key);
            if (node.HasValue && !overwrite)
                return;

            if (!node.HasValue)
                _count++;

            node.Value = value;
            node.HasValue = true;
        }
    }

    /// <inheritdoc />
    public ValueTask AddAsync(string key, TValue value, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Add(key, value, overwrite);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, out TValue value)
    {
        if (string.IsNullOrEmpty(key))
        {
            value = default!;
            return false;
        }

        lock (_gate)
        {
            var node = FindNode(key);
            if (node is { HasValue: true })
            {
                value = node.Value;
                return true;
            }
        }

        value = default!;
        return false;
    }

    /// <inheritdoc />
    public bool ContainsKey(string key) => TryGetValue(key, out _);

    /// <inheritdoc />
    public bool StartsWith(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return false;

        lock (_gate)
            return FindNode(prefix) is not null;
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        lock (_gate)
        {
            var path = new Stack<(Node Node, char Char)>();
            var node = _root;

            foreach (var ch in key)
            {
                if (!TryGetChild(node, ch, out var next))
                    return false;

                path.Push((node, ch));
                node = next;
            }

            if (!node.HasValue)
                return false;

            node.HasValue = false;
            node.Value = default!;
            _count--;

            while (path.Count > 0 && node.Children.Count == 0 && !node.HasValue)
            {
                var (parent, ch) = path.Pop();
                parent.Children.Remove(ch);
                node = parent;
            }

            return true;
        }
    }

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, TValue>> GetPrefixMatches(string prefix)
    {
        if (prefix is null)
            throw new ArgumentNullException(nameof(prefix));

        lock (_gate)
        {
            var node = FindNode(prefix);
            if (node is null)
                yield break;

            foreach (var item in Enumerate(node, prefix))
                yield return item;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_gate)
        {
            _root.Children.Clear();
            _root.HasValue = false;
            _root.Value = default!;
            _count = 0;
        }
    }

    private Node GetOrCreateNode(string key)
    {
        var node = _root;
        foreach (var ch in key)
        {
            if (!TryGetChild(node, ch, out var next))
            {
                next = new Node();
                node.Children[ch] = next;
            }

            node = next;
        }

        return node;
    }

    private Node? FindNode(string key)
    {
        var node = _root;
        foreach (var ch in key)
        {
            if (!TryGetChild(node, ch, out var next))
                return null;

            node = next;
        }

        return node;
    }

    private bool TryGetChild(Node node, char ch, out Node next)
    {
        foreach (var pair in node.Children)
        {
            if (_comparer.Equals(pair.Key.ToString(), ch.ToString()))
            {
                next = pair.Value;
                return true;
            }
        }

        next = null!;
        return false;
    }

    private IEnumerable<KeyValuePair<string, TValue>> Enumerate(Node node, string prefix)
    {
        var stack = new Stack<(Node Node, string Key)>();
        stack.Push((node, prefix));

        while (stack.Count > 0)
        {
            var (current, key) = stack.Pop();
            if (current.HasValue)
                yield return new KeyValuePair<string, TValue>(key, current.Value);

            foreach (var pair in current.Children)
                stack.Push((pair.Value, key + pair.Key));
        }
    }

    private sealed class Node
    {
        public Dictionary<char, Node> Children { get; } = new();
        public bool HasValue { get; set; }
        public TValue Value { get; set; } = default!;
    }
}
