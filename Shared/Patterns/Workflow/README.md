# Workflow<TContext>

Reusable workflow engine for sequential, conditional, parallel, retry, and compensating execution.

## Folder structure

```text
Shared/Patterns/Workflow
├── Abstractions
├── Builders
├── Configuration
├── Extensions
├── Internal
├── Models
└── README.md
```

## Capabilities

- sequential `Then` and `ThenAsync`
- conditional branching with `When`
- parallel fan-out with `Parallel`
- retry policies with `Retry`
- compensation with `Compensate`
- persistence abstraction via `IWorkflowPersistence<TContext>`
- DI registration via `AddWorkflowFramework`

## Example

```csharp
var workflow = Workflow<OrderContext>.CreateBuilder()
    .Then(ctx => ctx with { Total = ctx.Subtotal + ctx.Tax })
    .When(ctx => ctx.RequiresApproval, thenBranch =>
        thenBranch.Then(ctx => ctx with { Approved = true }))
    .Retry(async (ctx, token) =>
    {
        await Task.Delay(1, token);
        return ctx;
    })
    .Build();
```

## Complexity

- Construction: `O(1)`
- Sequential execution: `O(n)` for `n` steps
- Parallel execution: `O(m)` branch tasks plus combiner cost
- Retry: `O(r)` for `r` attempts
- Compensation rollback: `O(c)` for `c` registered compensations
