# Priority Queue

Reusable binary-heap priority queue with optional stable ordering.

## Example

```csharp
var queue = PriorityQueueExtensions.CreateBuilder<string, int>()
    .WithStableOrdering(true)
    .Build();

queue.Enqueue("low", 10);
queue.Enqueue("high", 1);
```

## Complexity

- Enqueue: `O(log n)`
- Dequeue: `O(log n)`
- Peek: `O(1)`
