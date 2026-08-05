# Circular Buffer

Reusable fixed-capacity ring buffer with overwrite-on-full semantics.

## Example

```csharp
var buffer = CircularBufferExtensions.CreateBuilder<int>()
    .WithCapacity(4)
    .Build();
```

## Complexity

- Enqueue: `O(1)`
- Dequeue: `O(1)`
- Peek: `O(1)`
- Snapshot: `O(n)`
