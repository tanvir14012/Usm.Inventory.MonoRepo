# Inbox

Reusable inbox abstraction for deduplicating, idempotently processing, and expiring inbound messages.

## Folder structure

```text
Shared/Patterns/Inbox
├── Abstractions
├── Builders
├── Configuration
├── Extensions
├── Models
└── README.md
```

## Capabilities

- deduplication by configurable key selector
- idempotent message handling
- expiration and cleanup
- storage abstraction
- handler abstraction
- DI registration via `AddInboxFramework`

## Example

```csharp
var inbox = new InboxBuilder<OrderPlaced, string>()
    .WithKeySelector(message => message.MessageId)
    .WithHandler(new OrderPlacedHandler())
    .WithRetention(TimeSpan.FromDays(7))
    .Build();

await inbox.ProcessAsync(message);
await inbox.CleanupExpiredAsync();
```

## Complexity

- Process: `O(1)` average
- Cleanup: `O(n)` for `n` tracked keys
