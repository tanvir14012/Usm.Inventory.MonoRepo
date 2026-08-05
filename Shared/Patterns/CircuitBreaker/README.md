# CircuitBreaker<TContext, TResult>

Reusable circuit breaker policy with closed, open, and half-open states, failure thresholds, timeouts, and metrics.

## Folder structure

```text
Shared/Patterns/CircuitBreaker
├── Abstractions
├── Builders
├── Configuration
├── Extensions
├── Models
└── README.md
```

## Capabilities

- closed, open, and half-open states
- failure thresholds
- open durations
- execution timeouts
- half-open permits
- metrics
- DI registration via `AddCircuitBreakerFramework`

## Example

```csharp
var breaker = new CircuitBreakerBuilder<HttpContext, HttpResponse>()
    .WithFailureThreshold(5)
    .WithOpenDuration(TimeSpan.FromSeconds(30))
    .WithExecutionTimeout(TimeSpan.FromSeconds(2))
    .Build();

var response = await breaker.ExecuteAsync(context, async (ctx, token) =>
{
    return await client.SendAsync(ctx, token);
});
```

## Complexity

- State check: `O(1)`
- Execution: `O(1)` excluding user code
- Open-state recovery: `O(1)`
