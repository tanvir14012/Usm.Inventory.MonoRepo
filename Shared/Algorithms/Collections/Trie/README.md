# Trie

Reusable string trie with prefix enumeration and generic value storage.

## Example

```csharp
var trie = TrieExtensions.CreateBuilder<int>().Build();
trie.Add("cat", 1);
trie.Add("car", 2);
```

## Complexity

- Add: `O(k)`
- TryGetValue: `O(k)`
- Remove: `O(k)`
- Prefix enumeration: `O(k + m)`

Where `k` is the key length and `m` is the number of matched nodes.
