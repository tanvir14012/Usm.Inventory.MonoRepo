# Compression Algorithms

Reusable compression and encoding algorithms.

## Example

```csharp
var alg = CompressionAlgorithmsExtensions.CreateBuilder().Build();
var encoded = alg.RunLengthEncode("aaabbb");
var (huffman, map) = alg.HuffmanEncode("hello");
var delta = alg.DeltaEncode(new byte[] { 1, 3, 5, 7 });
```

## Complexity

- Run-Length: Encode `O(n)`, Decode `O(n)`
- Huffman: Encode `O(n log n)`, Decode `O(n)`
- Delta: `O(n)`
