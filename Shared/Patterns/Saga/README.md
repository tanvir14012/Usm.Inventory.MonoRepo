# Saga

Reusable saga orchestration for step execution, rollback compensation, and snapshot persistence.

## Folder structure

```text
Shared/Patterns/Saga
├── Abstractions
├── Builders
├── Configuration
├── Extensions
├── Models
└── README.md
```

## Capabilities

- sequential saga steps
- compensation rollback
- snapshot persistence abstraction
- in-memory persistence for tests
- DI registration via `AddSagaFramework`

## Example

```csharp
var saga = new SagaBuilder<OrderContext>()
    .WithSagaId("order-checkout")
    .Use((ctx, ct) => ctx.ReserveInventoryAsync(ct), (ctx, ct) => ctx.ReleaseInventoryAsync(ct))
    .Use((ctx, ct) => ctx.AuthorizePaymentAsync(ct), (ctx, ct) => ctx.VoidPaymentAsync(ct))
    .Build();

var result = await saga.ExecuteAsync(context);
```

## Complexity

- Execute: `O(n)`
- Rollback: `O(k)` for `k` completed compensable steps
