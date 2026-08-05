using Usm.Shared.Algorithms.Sorting.Abstractions;

namespace Usm.Shared.Algorithms.Sorting;

/// <summary>
/// Generic sorting algorithm set.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class SortingAlgorithms<T> : ISortingAlgorithms<T>
    where T : notnull
{
    private const int IntroSortInsertionThreshold = 16;

    /// <summary>Initializes a new sorting algorithm set.</summary>
    public SortingAlgorithms(SortingOptions<T>? options = null)
    {
        Comparer = options?.Comparer ?? Comparer<T>.Default;
    }

    /// <inheritdoc />
    public IComparer<T> Comparer { get; }

    /// <inheritdoc />
    public void QuickSort(T[] items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Length <= 1)
            return;

        QuickSort(items, 0, items.Length - 1);
    }

    /// <inheritdoc />
    public ValueTask QuickSortAsync(T[] items, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        QuickSort(items);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void MergeSort(T[] items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Length <= 1)
            return;

        var buffer = new T[items.Length];
        MergeSort(items, buffer, 0, items.Length - 1);
    }

    /// <inheritdoc />
    public ValueTask MergeSortAsync(T[] items, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MergeSort(items);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void HeapSort(T[] items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var count = items.Length;
        for (var i = count / 2 - 1; i >= 0; i--)
            SiftDown(items, count, i);

        for (var end = count - 1; end > 0; end--)
        {
            (items[0], items[end]) = (items[end], items[0]);
            SiftDown(items, end, 0);
        }
    }

    /// <inheritdoc />
    public ValueTask HeapSortAsync(T[] items, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        HeapSort(items);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void IntroSort(T[] items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Length <= 1)
            return;

        var depthLimit = 2 * FloorLog2(items.Length);
        IntroSort(items, 0, items.Length - 1, depthLimit);
    }

    /// <inheritdoc />
    public ValueTask IntroSortAsync(T[] items, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IntroSort(items);
        return ValueTask.CompletedTask;
    }

    private void QuickSort(T[] items, int left, int right)
    {
        if (left >= right)
            return;

        var pivotIndex = Partition(items, left, right);
        QuickSort(items, left, pivotIndex - 1);
        QuickSort(items, pivotIndex + 1, right);
    }

    private int Partition(T[] items, int left, int right)
    {
        var pivot = items[right];
        var storeIndex = left;
        for (var i = left; i < right; i++)
        {
            if (Comparer.Compare(items[i], pivot) <= 0)
            {
                (items[storeIndex], items[i]) = (items[i], items[storeIndex]);
                storeIndex++;
            }
        }

        (items[storeIndex], items[right]) = (items[right], items[storeIndex]);
        return storeIndex;
    }

    private void MergeSort(T[] items, T[] buffer, int left, int right)
    {
        if (left >= right)
            return;

        var mid = left + ((right - left) >> 1);
        MergeSort(items, buffer, left, mid);
        MergeSort(items, buffer, mid + 1, right);
        Merge(items, buffer, left, mid, right);
    }

    private void Merge(T[] items, T[] buffer, int left, int mid, int right)
    {
        Array.Copy(items, left, buffer, left, right - left + 1);

        var i = left;
        var j = mid + 1;
        var k = left;

        while (i <= mid && j <= right)
        {
            if (Comparer.Compare(buffer[i], buffer[j]) <= 0)
                items[k++] = buffer[i++];
            else
                items[k++] = buffer[j++];
        }

        while (i <= mid)
            items[k++] = buffer[i++];

        while (j <= right)
            items[k++] = buffer[j++];
    }

    private void SiftDown(T[] items, int count, int root)
    {
        while (true)
        {
            var leftChild = root * 2 + 1;
            if (leftChild >= count)
                return;

            var largest = root;
            if (Comparer.Compare(items[largest], items[leftChild]) < 0)
                largest = leftChild;

            var rightChild = leftChild + 1;
            if (rightChild < count && Comparer.Compare(items[largest], items[rightChild]) < 0)
                largest = rightChild;

            if (largest == root)
                return;

            (items[root], items[largest]) = (items[largest], items[root]);
            root = largest;
        }
    }

    private void IntroSort(T[] items, int left, int right, int depthLimit)
    {
        while (right - left > IntroSortInsertionThreshold)
        {
            if (depthLimit == 0)
            {
                HeapSortRange(items, left, right);
                return;
            }

            depthLimit--;
            var pivotIndex = Partition(items, left, right);
            IntroSort(items, pivotIndex + 1, right, depthLimit);
            right = pivotIndex - 1;
        }

        InsertionSort(items, left, right);
    }

    private void HeapSortRange(T[] items, int left, int right)
    {
        var count = right - left + 1;
        for (var i = count / 2 - 1; i >= 0; i--)
            SiftDownRange(items, left, count, i);

        for (var end = count - 1; end > 0; end--)
        {
            (items[left], items[left + end]) = (items[left + end], items[left]);
            SiftDownRange(items, left, end, 0);
        }
    }

    private void SiftDownRange(T[] items, int offset, int count, int root)
    {
        while (true)
        {
            var leftChild = root * 2 + 1;
            if (leftChild >= count)
                return;

            var largest = root;
            if (Comparer.Compare(items[offset + largest], items[offset + leftChild]) < 0)
                largest = leftChild;

            var rightChild = leftChild + 1;
            if (rightChild < count && Comparer.Compare(items[offset + largest], items[offset + rightChild]) < 0)
                largest = rightChild;

            if (largest == root)
                return;

            (items[offset + root], items[offset + largest]) = (items[offset + largest], items[offset + root]);
            root = largest;
        }
    }

    private void InsertionSort(T[] items, int left, int right)
    {
        for (var i = left + 1; i <= right; i++)
        {
            var value = items[i];
            var j = i - 1;
            while (j >= left && Comparer.Compare(items[j], value) > 0)
            {
                items[j + 1] = items[j];
                j--;
            }

            items[j + 1] = value;
        }
    }

    private static int FloorLog2(int value)
    {
        var result = 0;
        while (value > 1)
        {
            value >>= 1;
            result++;
        }

        return result;
    }
}
