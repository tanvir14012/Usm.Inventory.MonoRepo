using Usm.Shared.Algorithms.Searching.Abstractions;

namespace Usm.Shared.Algorithms.Searching.Builders;

/// <summary>
/// Fluent builder for search algorithms.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class SearchAlgorithmsBuilder<T> : ISearchAlgorithmsBuilder<T>
    where T : notnull
{
    private readonly SearchOptions<T> _options = new();

    /// <inheritdoc />
    public ISearchAlgorithmsBuilder<T> WithComparer(IComparer<T> comparer)
    {
        _options.Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        return this;
    }

    /// <inheritdoc />
    public ISearchAlgorithms<T> Build() => new SearchAlgorithms<T>(_options);
}
