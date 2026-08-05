using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Algorithms.Sorting.Abstractions;
using Usm.Shared.Algorithms.Sorting.Builders;

namespace Usm.Shared.Algorithms.Sorting.Extensions;

/// <summary>
/// Common extension methods for sorting algorithm creation and numeric variants.
/// </summary>
public static class SortingAlgorithmsExtensions
{
    /// <summary>Creates a new builder.</summary>
    public static ISortingAlgorithmsBuilder<T> CreateBuilder<T>()
        where T : notnull
        => new SortingAlgorithmsBuilder<T>();

    /// <summary>Registers the builder.</summary>
    public static IServiceCollection AddSortingAlgorithmsFramework(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(SortingAlgorithmsBuilder<>), typeof(SortingAlgorithmsBuilder<>));
        return services;
    }

    /// <summary>Sorts integers using counting sort.</summary>
    public static ValueTask CountingSortAsync<T>(this ISortingAlgorithms<T> algorithms, T[] items, CancellationToken cancellationToken = default)
        where T : IBinaryInteger<T>
    {
        cancellationToken.ThrowIfCancellationRequested();
        algorithms.CountingSort(items);
        return ValueTask.CompletedTask;
    }

    /// <summary>Sorts integers using counting sort.</summary>
    public static void CountingSort<T>(this ISortingAlgorithms<T> algorithms, T[] items)
        where T : IBinaryInteger<T>
    {
        ArgumentNullException.ThrowIfNull(algorithms);
        ArgumentNullException.ThrowIfNull(items);
        if (items.Length <= 1)
            return;

        var min = items[0];
        var max = items[0];
        foreach (var item in items)
        {
            if (algorithms.Comparer.Compare(item, min) < 0)
                min = item;
            if (algorithms.Comparer.Compare(item, max) > 0)
                max = item;
        }

        var range = max - min;
        var countLength = int.CreateChecked(range) + 1;
        var counts = new int[countLength];

        foreach (var item in items)
            counts[int.CreateChecked(item - min)]++;

        for (var i = 1; i < counts.Length; i++)
            counts[i] += counts[i - 1];

        var output = new T[items.Length];
        for (var i = items.Length - 1; i >= 0; i--)
        {
            var index = int.CreateChecked(items[i] - min);
            output[--counts[index]] = items[i];
        }

        Array.Copy(output, items, items.Length);
    }

    /// <summary>Sorts integers using radix sort.</summary>
    public static void RadixSort<T>(this ISortingAlgorithms<T> algorithms, T[] items)
        where T : IBinaryInteger<T>
    {
        ArgumentNullException.ThrowIfNull(algorithms);
        ArgumentNullException.ThrowIfNull(items);
        if (items.Length <= 1)
            return;

        var min = items[0];
        foreach (var item in items)
        {
            if (algorithms.Comparer.Compare(item, min) < 0)
                min = item;
        }

        var offset = min < T.Zero ? -min : T.Zero;
        var max = T.Zero;
        var adjusted = new T[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            adjusted[i] = items[i] + offset;
            if (adjusted[i] > max)
                max = adjusted[i];
        }

        var output = new T[items.Length];
        var exp = T.One;
        var baseValue = T.CreateChecked(10);

        while (max / exp > T.Zero)
        {
            var counts = new int[10];
            for (var i = 0; i < adjusted.Length; i++)
            {
                var digit = int.CreateChecked((adjusted[i] / exp) % baseValue);
                counts[digit]++;
            }

            for (var i = 1; i < counts.Length; i++)
                counts[i] += counts[i - 1];

            for (var i = adjusted.Length - 1; i >= 0; i--)
            {
                var digit = int.CreateChecked((adjusted[i] / exp) % baseValue);
                output[--counts[digit]] = adjusted[i];
            }

            (adjusted, output) = (output, adjusted);
            exp *= baseValue;
        }

        for (var i = 0; i < items.Length; i++)
            items[i] = adjusted[i] - offset;
    }

    /// <summary>Sorts floating point numbers using bucket sort.</summary>
    public static void BucketSort<T>(this ISortingAlgorithms<T> algorithms, T[] items)
        where T : IFloatingPointIeee754<T>
    {
        ArgumentNullException.ThrowIfNull(algorithms);
        ArgumentNullException.ThrowIfNull(items);
        if (items.Length <= 1)
            return;

        var min = items[0];
        var max = items[0];
        foreach (var item in items)
        {
            if (algorithms.Comparer.Compare(item, min) < 0)
                min = item;
            if (algorithms.Comparer.Compare(item, max) > 0)
                max = item;
        }

        var range = max - min;
        if (range == T.Zero)
            return;

        var bucketCount = items.Length;
        var buckets = new List<T>[bucketCount];
        for (var i = 0; i < bucketCount; i++)
            buckets[i] = new List<T>();

        var scale = T.CreateChecked(bucketCount - 1);
        foreach (var item in items)
        {
            var normalized = (item - min) / range;
            var bucketIndex = int.CreateChecked(normalized * scale);
            buckets[bucketIndex].Add(item);
        }

        var index = 0;
        foreach (var bucket in buckets)
        {
            bucket.Sort(algorithms.Comparer);
            foreach (var item in bucket)
                items[index++] = item;
        }
    }

    /// <summary>Sorts floating point numbers using bucket sort asynchronously.</summary>
    public static ValueTask BucketSortAsync<T>(this ISortingAlgorithms<T> algorithms, T[] items, CancellationToken cancellationToken = default)
        where T : IFloatingPointIeee754<T>
    {
        cancellationToken.ThrowIfCancellationRequested();
        algorithms.BucketSort(items);
        return ValueTask.CompletedTask;
    }
}
