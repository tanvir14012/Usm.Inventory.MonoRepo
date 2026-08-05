# Pipeline<TContext>

Reusable ordered pipeline for context transformation with sync, async, and finalizer support.

## Folder structure

```text
Shared/Patterns/Pipeline
├── Abstractions
├── Builders
├── Configuration
├── Extensions
├── Internal
└── README.md
```

## Capabilities

- `Use`, `Then`, `Finally`
- `Execute` and `ExecuteAsync`
- `ToExpression` and `Compile`
- builder-based composition
- DI registration via `AddPipelineFramework`
- compiled delegate caching through `PipelineOptions`

## Example

```csharp
var pipeline = Pipeline<InvoiceContext>.CreateBuilder()
    .Use(ctx => new InvoiceContext(ctx.Id, ctx.Amount + ctx.Tax, ctx.Tax))
    .Then(ctx => new InvoiceContext(ctx.Id, decimal.Round(ctx.Amount, 2), ctx.Tax))
    .Finally(ctx => logger.LogInformation("Processed invoice {Id}", ctx.Id))
    .Build();

var result = await pipeline.ExecuteAsync(new InvoiceContext(1, 100m, 15m));
```

## Complexity

- Construction: `O(1)`
- Sync execution: `O(n)` for `n` steps
- Async execution: `O(n)` for `n` steps
- Expression compilation: `O(n)` in expression-tree size
