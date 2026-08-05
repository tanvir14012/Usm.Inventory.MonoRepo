# Searching Algorithms

Reusable sorted-sequence search algorithms.

## Example

```csharp
var search = SearchAlgorithmsExtensions.CreateBuilder<int>().Build();
var index = search.BinarySearch(new[] { 1, 3, 5, 7 }, 5);
```

## Complexity

- Binary search: `O(log n)`
- Jump search: `O(sqrt(n))`
- Exponential search: `O(log n)`
- Interpolation search: `O(log log n)` average, `O(n)` worst case
