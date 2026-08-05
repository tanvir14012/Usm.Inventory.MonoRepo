# Disjoint Set

Reusable union-find data structure with path compression and union by rank.

## Example

```csharp
var set = new DisjointSetBuilder<int>().Build();
set.Add(1);
set.Add(2);
set.Union(1, 2);
```

## Complexity

- Add: `O(1)`
- Find: amortized `O(alpha(n))`
- Union: amortized `O(alpha(n))`
- Connected: amortized `O(alpha(n))`
