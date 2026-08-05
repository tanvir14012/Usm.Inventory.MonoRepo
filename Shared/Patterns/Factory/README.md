# Factory<TContext, TProduct>

Reusable contextual factory abstraction for synchronous, asynchronous, and expression-backed creation.

## Folder structure

```text
Shared/Patterns/Factory
├── Abstractions
├── Builders
├── Configuration
├── Extensions
├── Internal
└── README.md
```

## Capabilities

- `Create` and `CreateAsync`
- `ToExpression` and `Compile`
- builder-based configuration
- DI registration via `AddFactoryFramework`
- compiled delegate caching through `FactoryOptions`

## Example

```csharp
var factory = Factory<OrderContext, OrderDto>.From(ctx => new OrderDto(ctx.Id, ctx.Total));
var dto = factory.Create(new OrderContext(1, 125m));

var asyncFactory = Factory<OrderContext, OrderDto>.FromAsync(async (ctx, token) =>
{
    await Task.Delay(1, token);
    return new OrderDto(ctx.Id, ctx.Total);
});

var builder = Factory<OrderContext, OrderDto>.CreateBuilder();
var built = builder.UseExpression(ctx => new OrderDto(ctx.Id, ctx.Total)).Build();
```

## Complexity

- Construction: `O(1)`
- Sync creation: `O(1)` excluding user code
- Async creation: `O(1)` excluding user code
- Expression compilation: `O(n)` in expression-tree size
