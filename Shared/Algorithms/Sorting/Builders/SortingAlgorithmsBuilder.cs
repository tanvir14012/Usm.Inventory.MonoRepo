using Usm.Shared.Algorithms.Sorting.Abstractions;

namespace Usm.Shared.Algorithms.Sorting.Builders;

/// <summary>
/// Fluent builder for sorting algorithms.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class SortingAlgorithmsBuilder<T> : ISortingAlgorithmsBuilder<T>
    where T : notnull
{
    private readonly SortingOptions<T> _options = new();

    /// <inheritdoc />
    public ISortingAlgorithmsBuilder<T> WithComparer(IComparer<T> comparer)
    {
        _options.Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        return this;
    }

    /// <inheritdoc />
    public ISortingAlgorithms<T> Build() => new SortingAlgorithms<T>(_options);
}
