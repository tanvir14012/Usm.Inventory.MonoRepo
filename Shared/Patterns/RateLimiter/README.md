# RateLimiter

Reusable generic rate limiter with token bucket, fixed window, sliding window, and leaky bucket algorithms.

## Folder structure

```text
Shared/Patterns/RateLimiter
├── Abstractions
├── Builders
├── Configuration
├── Extensions
├── Implementation
├── Models
└── README.md
```

## Capabilities

- async permit acquisition
- token bucket
- fixed window
- sliding window
- leaky bucket
- builder and DI registration
- `TimeProvider` support

## Example

```csharp
var limiter = new RateLimiterBuilder<string>()
    .WithAlgorithm(RateLimiterAlgorithm.TokenBucket)
    .WithPermitLimit(100)
    .WithWindow(TimeSpan.FromMinutes(1))
    .Build();

var lease = await limiter.AcquireAsync("checkout", permits: 5);
```

## Complexity

- Acquire: `O(1)` for token/fixed/leaky, `O(s)` for sliding window with `s` segments
- Memory: `O(s)` for sliding window, `O(1)` otherwise
