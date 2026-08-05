using System.Collections.Generic;

namespace Usm.Shared.Algorithms.Collections.BTree.Abstractions;

/// <summary>
/// Represents a reusable B-tree.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public interface IBTree<TKey, TValue>
    where TKey : notnull
{
    /// <summary>Gets the comparer used to order keys.</summary>
    IComparer<TKey> Comparer { get; }

    /// <summary>Gets the minimum degree.</summary>
    int MinimumDegree { get; }

    /// <summary>Gets the number of stored keys.</summary>
    int Count { get; }

    /// <summary>Adds or updates a key.</summary>
    void Add(TKey key, TValue value);

    /// <summary>Adds or updates a key asynchronously.</summary>
    ValueTask AddAsync(TKey key, TValue value, CancellationToken cancellationToken = default);

    /// <summary>Determines whether the key exists.</summary>
    bool ContainsKey(TKey key);

    /// <summary>Determines whether the key exists asynchronously.</summary>
    ValueTask<bool> ContainsKeyAsync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>Attempts to read a value.</summary>
    bool TryGetValue(TKey key, out TValue value);

    /// <summary>Attempts to read a value asynchronously.</summary>
    ValueTask<(bool Found, TValue Value)> TryGetValueAsync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>Enumerates keys in ascending order.</summary>
    IEnumerable<KeyValuePair<TKey, TValue>> Traverse();

    /// <summary>Enumerates keys in ascending order asynchronously.</summary>
    IAsyncEnumerable<KeyValuePair<TKey, TValue>> TraverseAsync(CancellationToken cancellationToken = default);

    /// <summary>Clears the tree.</summary>
    void Clear();
}
