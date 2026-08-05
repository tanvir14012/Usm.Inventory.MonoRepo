# Interval Tree

Generic AVL-backed interval tree for overlap queries.

## Example

```csharp
var tree = IntervalTreeExtensions.CreateBuilder<int, string>().Build();
tree.Add(10, 20, "alpha");
tree.Add(15, 25, "beta");

var overlaps = tree.QueryPoint(18);
```

## Complexity

- Add: `O(log n)`
- Remove: `O(log n)`
- Query overlap: `O(log n + k)` average, `O(n)` worst case
- Space: `O(n)`
