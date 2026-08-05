# KD Tree

Generic 2D KD-tree for nearest-neighbor and range queries.

## Example

```csharp
var tree = KdTreeExtensions.CreateBuilder<double, string>().Build();
tree.Add(10, 20, "alpha");
tree.Add(12, 18, "beta");

var nearest = tree.NearestNeighbor(11, 19);
```

## Complexity

- Add: `O(log n)` average
- Remove: `O(log n)` average
- Nearest neighbor: `O(log n)` average, `O(n)` worst case
- Range query: `O(log n + k)` average, `O(n)` worst case
- Space: `O(n)`
