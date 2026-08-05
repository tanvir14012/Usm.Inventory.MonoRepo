# Fenwick Tree

Reusable binary indexed tree for prefix and range sums over generic numeric types.

## Example

```csharp
var tree = FenwickTreeExtensions.CreateBuilder<int>()
    .WithLength(10)
    .Build();

tree.Add(0, 5);
tree.Add(3, 2);
```

## Complexity

- Add: `O(log n)`
- Prefix sum: `O(log n)`
- Range sum: `O(log n)`
- Clear: `O(n)`
