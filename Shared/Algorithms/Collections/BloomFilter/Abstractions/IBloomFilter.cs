namespace Usm.Shared.Algorithms.Collections.BloomFilter.Abstractions;

/// <summary>
/// Represents a reusable Bloom filter.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public interface IBloomFilter<T>
    where T : notnull
{
    /// <summary>Gets the number of tracked insertions.</summary>
    int Count { get; }

    /// <summary>Adds an item to the filter.</summary>
    void Add(T item);

    /// <summary>Adds an item asynchronously.</summary>
    ValueTask AddAsync(T item, CancellationToken cancellationToken = default);

    /// <summary>Determines whether the item might be present.</summary>
    bool MightContain(T item);

    /// <summary>Determines whether the item might be present asynchronously.</summary>
    ValueTask<bool> MightContainAsync(T item, CancellationToken cancellationToken = default);

    /// <summary>Clears the filter.</summary>
    void Clear();
}
