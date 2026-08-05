# B Tree

Generic B-tree for ordered key/value storage and lookups.

## Example

```csharp
var tree = BTreeExtensions.CreateBuilder<int, string>().WithMinimumDegree(4).Build();
tree.Add(10, "alpha");
tree.Add(20, "beta");
```

## Complexity

- Insert: `O(log n)`
- Search: `O(log n)`
- Traverse: `O(n)`
- Space: `O(n)`
