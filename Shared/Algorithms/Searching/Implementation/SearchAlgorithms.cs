using Usm.Shared.Algorithms.Searching.Abstractions;

namespace Usm.Shared.Algorithms.Searching;

/// <summary>
/// Generic search algorithm set for sorted sequences.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class SearchAlgorithms<T> : ISearchAlgorithms<T>
    where T : notnull
{
    /// <summary>Initializes a new search algorithm set.</summary>
    public SearchAlgorithms(SearchOptions<T>? options = null)
    {
        Comparer = options?.Comparer ?? Comparer<T>.Default;
    }

    /// <inheritdoc />
    public IComparer<T> Comparer { get; }

    /// <inheritdoc />
    public int BinarySearch(IReadOnlyList<T> items, T target)
    {
        ArgumentNullException.ThrowIfNull(items);
        var low = 0;
        var high = items.Count - 1;

        while (low <= high)
        {
            var mid = low + ((high - low) >> 1);
            var comparison = Comparer.Compare(items[mid], target);
            if (comparison == 0)
                return mid;

            if (comparison < 0)
                low = mid + 1;
            else
                high = mid - 1;
        }

        return -1;
    }

    /// <inheritdoc />
    public ValueTask<int> BinarySearchAsync(IReadOnlyList<T> items, T target, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(BinarySearch(items, target));
    }

    /// <inheritdoc />
    public int JumpSearch(IReadOnlyList<T> items, T target)
    {
        ArgumentNullException.ThrowIfNull(items);
        var count = items.Count;
        if (count == 0)
            return -1;

        var step = Math.Max(1, (int)Math.Sqrt(count));
        var prev = 0;
        var current = step;

        while (prev < count && Comparer.Compare(items[Math.Min(current, count) - 1], target) < 0)
        {
            prev = current;
            current += step;
            if (prev >= count)
                return -1;
        }

        for (var i = prev; i < Math.Min(current, count); i++)
        {
            var comparison = Comparer.Compare(items[i], target);
            if (comparison == 0)
                return i;
            if (comparison > 0)
                break;
        }

        return -1;
    }

    /// <inheritdoc />
    public ValueTask<int> JumpSearchAsync(IReadOnlyList<T> items, T target, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(JumpSearch(items, target));
    }

    /// <inheritdoc />
    public int ExponentialSearch(IReadOnlyList<T> items, T target)
    {
        ArgumentNullException.ThrowIfNull(items);
        var count = items.Count;
        if (count == 0)
            return -1;

        if (Comparer.Compare(items[0], target) == 0)
            return 0;

        var bound = 1;
        while (bound < count && Comparer.Compare(items[bound], target) < 0)
            bound <<= 1;

        var low = bound >> 1;
        var high = Math.Min(bound, count - 1);
        return BinarySearch(items, target, low, high);
    }

    /// <inheritdoc />
    public ValueTask<int> ExponentialSearchAsync(IReadOnlyList<T> items, T target, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ExponentialSearch(items, target));
    }

    private int BinarySearch(IReadOnlyList<T> items, T target, int low, int high)
    {
        while (low <= high)
        {
            var mid = low + ((high - low) >> 1);
            var comparison = Comparer.Compare(items[mid], target);
            if (comparison == 0)
                return mid;

            if (comparison < 0)
                low = mid + 1;
            else
                high = mid - 1;
        }

        return -1;
    }
}
