namespace Usm.Shared.Algorithms.Collections.Trie.Abstractions;

/// <summary>
/// Represents a reusable trie for string keys.
/// </summary>
/// <typeparam name="TValue">The stored value type.</typeparam>
public interface ITrie<TValue>
{
    /// <summary>Gets the number of stored keys.</summary>
    int Count { get; }

    /// <summary>Adds or replaces a value at the specified key.</summary>
    void Add(string key, TValue value, bool overwrite = true);

    /// <summary>Adds or replaces a value asynchronously.</summary>
    ValueTask AddAsync(string key, TValue value, bool overwrite = true, CancellationToken cancellationToken = default);

    /// <summary>Attempts to retrieve a value by key.</summary>
    bool TryGetValue(string key, out TValue value);

    /// <summary>Determines whether a key exists.</summary>
    bool ContainsKey(string key);

    /// <summary>Determines whether any key uses the supplied prefix.</summary>
    bool StartsWith(string prefix);

    /// <summary>Removes a key.</summary>
    bool Remove(string key);

    /// <summary>Gets values matching a prefix.</summary>
    IEnumerable<KeyValuePair<string, TValue>> GetPrefixMatches(string prefix);

    /// <summary>Clears the trie.</summary>
    void Clear();
}
