# Retry

Reusable retry policy for synchronous and asynchronous operations with fixed, linear, exponential, and custom delay strategies.

## Folder structure

```text
Shared/Patterns/Retry
├── Abstractions
├── Builders
├── Configuration
├── Extensions
└── README.md
```

## Capabilities

- fixed, linear, exponential, and custom delays
- jitter support
- sync and async execution
- `TimeProvider`-based delays for testability
- DI registration via `AddRetryFramework`

## Example

```csharp
var policy = new RetryBuilder<HttpContext, HttpResponse>()
    .WithMaxAttempts(5)
    .WithStrategy(RetryStrategy.Exponential)
    .WithJitter(true)
    .Build();

var response = await policy.ExecuteAsync(context, async (ctx, token) =>
{
    return await client.SendAsync(ctx, token);
});
```

## Complexity

- Retry execution: `O(a)` for `a` attempts
- Delay computation: `O(1)`
- Jitter calculation: `O(1)`
