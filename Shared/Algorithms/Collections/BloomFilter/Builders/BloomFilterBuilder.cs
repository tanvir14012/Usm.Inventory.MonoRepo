using Usm.Shared.Algorithms.Collections.BloomFilter.Abstractions;

namespace Usm.Shared.Algorithms.Collections.BloomFilter.Builders;

/// <summary>
/// Fluent builder for Bloom filter configuration.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class BloomFilterBuilder<T> : IBloomFilterBuilder<T>
    where T : notnull
{
    private int _expectedItemCount = 1000;
    private double _falsePositiveRate = 0.01d;
    private IEqualityComparer<T>? _comparer;

    /// <inheritdoc />
    public IBloomFilterBuilder<T> WithExpectedItemCount(int count)
    {
        _expectedItemCount = count > 0 ? count : throw new ArgumentOutOfRangeException(nameof(count));
        return this;
    }

    /// <inheritdoc />
    public IBloomFilterBuilder<T> WithFalsePositiveRate(double rate)
    {
        _falsePositiveRate = rate > 0 && rate < 1 ? rate : throw new ArgumentOutOfRangeException(nameof(rate));
        return this;
    }

    /// <inheritdoc />
    public IBloomFilterBuilder<T> WithComparer(IEqualityComparer<T> comparer)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        return this;
    }

    /// <inheritdoc />
    public IBloomFilter<T> Build()
        => new BloomFilter<T>(_expectedItemCount, _falsePositiveRate, _comparer ?? EqualityComparer<T>.Default);
}
