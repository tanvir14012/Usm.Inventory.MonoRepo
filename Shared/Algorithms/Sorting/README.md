# Sorting Algorithms

Reusable sorting algorithms for arrays.

## Example

```csharp
var sorting = SortingAlgorithmsExtensions.CreateBuilder<int>().Build();
var values = new[] { 3, 1, 2 };
sorting.IntroSort(values);
```

## Complexity

- Quick sort: `O(n log n)` average, `O(n^2)` worst case
- Merge sort: `O(n log n)`
- Heap sort: `O(n log n)`
- Intro sort: `O(n log n)` average
- Counting sort: `O(n + k)`
- Radix sort: `O(d(n + k))`
- Bucket sort: `O(n + k)` average
