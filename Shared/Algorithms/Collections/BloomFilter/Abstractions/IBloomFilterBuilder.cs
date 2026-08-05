namespace Usm.Shared.Algorithms.Collections.BloomFilter.Abstractions;

/// <summary>
/// Fluent builder for Bloom filter configuration.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public interface IBloomFilterBuilder<T>
    where T : notnull
{
    /// <summary>Sets the expected item count.</summary>
    IBloomFilterBuilder<T> WithExpectedItemCount(int count);

    /// <summary>Sets the target false positive rate.</summary>
    IBloomFilterBuilder<T> WithFalsePositiveRate(double rate);

    /// <summary>Sets the comparer used for item hashing.</summary>
    IBloomFilterBuilder<T> WithComparer(IEqualityComparer<T> comparer);

    /// <summary>Builds the filter.</summary>
    IBloomFilter<T> Build();
}
