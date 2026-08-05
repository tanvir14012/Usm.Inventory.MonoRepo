using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Algorithms.Searching.Abstractions;
using Usm.Shared.Algorithms.Searching.Builders;

namespace Usm.Shared.Algorithms.Searching.Extensions;

/// <summary>
/// Common extension methods for search algorithm creation.
/// </summary>
public static class SearchAlgorithmsExtensions
{
    /// <summary>Creates a new builder.</summary>
    public static ISearchAlgorithmsBuilder<T> CreateBuilder<T>()
        where T : notnull
        => new SearchAlgorithmsBuilder<T>();

    /// <summary>Registers the builder.</summary>
    public static IServiceCollection AddSearchAlgorithmsFramework(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(SearchAlgorithmsBuilder<>), typeof(SearchAlgorithmsBuilder<>));
        return services;
    }

    /// <summary>Performs interpolation search over a sorted numeric sequence.</summary>
    public static int InterpolationSearch<T>(this ISearchAlgorithms<T> algorithms, IReadOnlyList<T> items, T target)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(algorithms);
        ArgumentNullException.ThrowIfNull(items);

        var low = 0;
        var high = items.Count - 1;

        while (low <= high && algorithms.Comparer.Compare(target, items[low]) >= 0 && algorithms.Comparer.Compare(target, items[high]) <= 0)
        {
            if (low == high)
                return algorithms.Comparer.Compare(items[low], target) == 0 ? low : -1;

            var lowValue = items[low];
            var highValue = items[high];
            var denominator = highValue - lowValue;
            if (denominator == T.Zero)
                break;

            var numerator = (target - lowValue) * T.CreateChecked(high - low);
            var offset = numerator / denominator;
            var pos = low + int.CreateChecked(offset);
            if ((uint)pos >= (uint)items.Count)
                break;

            var comparison = algorithms.Comparer.Compare(items[pos], target);
            if (comparison == 0)
                return pos;

            if (comparison < 0)
                low = pos + 1;
            else
                high = pos - 1;
        }

        return -1;
    }

    /// <summary>Performs interpolation search asynchronously.</summary>
    public static ValueTask<int> InterpolationSearchAsync<T>(this ISearchAlgorithms<T> algorithms, IReadOnlyList<T> items, T target, CancellationToken cancellationToken = default)
        where T : INumber<T>
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(algorithms.InterpolationSearch(items, target));
    }
}
