# Strategy<TContext, TResult>

Reusable strategy abstraction for pluggable algorithms with sync, async, and expression-backed execution.

## Folder structure

```text
Shared/Patterns/Strategy
├── Abstractions
├── Builders
├── Configuration
├── Extensions
├── Internal
└── README.md
```

## Capabilities

- `Execute` and `ExecuteAsync`
- `ToExpression` and `Compile`
- builder-based configuration
- DI registration via `AddStrategyFramework`
- compiled delegate caching through `StrategyOptions`

## Example

```csharp
var strategy = Strategy<PriceContext, decimal>.From(ctx => ctx.BasePrice * (1 - ctx.Discount));
var total = strategy.Execute(new PriceContext(100m, 0.15m));

var asyncStrategy = Strategy<PriceContext, decimal>.FromAsync(async (ctx, token) =>
{
    await Task.Delay(1, token);
    return ctx.BasePrice * (1 - ctx.Discount);
});
```

## Complexity

- Construction: `O(1)`
- Sync execution: `O(1)` excluding user code
- Async execution: `O(1)` excluding user code
- Expression compilation: `O(n)` in expression-tree size
