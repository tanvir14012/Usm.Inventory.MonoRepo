# Segment Tree

Reusable segment tree for generic numeric range sums and point updates.

## Example

```csharp
var tree = SegmentTreeExtensions.CreateBuilder<int>()
    .WithLength(16)
    .Build();

tree.Add(0, 5);
tree.Add(3, 7);
```

## Complexity

- Add: `O(log n)`
- Query: `O(log n)`
- Clear: `O(n)`
