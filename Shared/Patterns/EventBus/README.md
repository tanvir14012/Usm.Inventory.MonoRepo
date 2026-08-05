# EventBus<TEvent>

Reusable asynchronous event bus with priority-ordered subscribers, middleware, and sequential or parallel dispatch.

## Folder structure

```text
Shared/Patterns/EventBus
├── Abstractions
├── Builders
├── Configuration
├── Extensions
└── README.md
```

## Capabilities

- sync and async subscription
- priority ordering
- middleware pipeline
- sequential or parallel dispatch
- DI registration via `AddEventBusFramework`

## Example

```csharp
var bus = new EventBusBuilder<OrderPlaced>()
    .SubscribeAsync(async (evt, token) => await handler.HandleAsync(evt, token), priority: 10)
    .Use((evt, next, token) => next(token))
    .Build();

await bus.PublishAsync(new OrderPlaced(orderId));
```

## Complexity

- Subscription registration: `O(1)`
- Sequential publish: `O(n)`
- Parallel publish: `O(n)` plus task scheduling
