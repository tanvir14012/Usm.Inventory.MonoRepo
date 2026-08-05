# String Algorithms

Reusable string matching and distance algorithms.

## Example

```csharp
var alg = StringAlgorithmsExtensions.CreateBuilder().Build();
var index = alg.KmpSearch("abcdef", "cde");
var distance = alg.LevenshteinDistance("kitten", "sitting");
```

## Complexity

- KMP: `O(n + m)`
- Rabin-Karp: `O(n + m)` average
- Boyer-Moore: `O(n/m)` best, `O(nm)` worst
- Levenshtein: `O(mn)`
- Damerau-Levenshtein: `O(mn)`
- LCS: `O(mn)`
