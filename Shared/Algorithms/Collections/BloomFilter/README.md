# Bloom Filter

Reusable probabilistic set membership filter with configurable capacity and false-positive rate.

## Example

```csharp
var filter = new BloomFilterBuilder<string>()
    .WithExpectedItemCount(10_000)
    .WithFalsePositiveRate(0.01)
    .Build();

filter.Add("hello");
var maybe = filter.MightContain("hello");
```

## Complexity

- Add: `O(k)`
- MightContain: `O(k)`
- Memory: `O(m)`

Where `k` is the number of hash functions and `m` is the bit-array size.
