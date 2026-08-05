# Outbox

Reusable outbox abstraction for storing messages, serializing them, and dispatching them reliably in batches.

## Folder structure

```text
Shared/Patterns/Outbox
├── Abstractions
├── Builders
├── Configuration
├── Extensions
├── Models
└── README.md
```

## Capabilities

- storage abstraction
- serializer abstraction
- dispatcher abstraction
- batch dispatching
- in-memory test store
- DI registration via `AddOutboxFramework`

## Example

```csharp
var outbox = new OutboxBuilder<OrderPlaced>()
    .WithDispatcher(new OrderPlacedDispatcher())
    .WithBatchSize(100)
    .Build();

await outbox.EnqueueAsync(new OrderPlaced(orderId));
await outbox.DispatchPendingAsync();
```

## Complexity

- Enqueue: `O(1)`
- Dispatch: `O(n)` for `n` pending messages
- Serialization: dependent on payload size
