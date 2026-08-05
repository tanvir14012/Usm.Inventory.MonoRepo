# Distributed Algorithms

Reusable distributed system utilities.

## Example

```csharp
var alg = DistributedAlgorithmsExtensions.CreateBuilder().Build();
var hash = alg.ConsistentHash("key", 100);
var id = alg.SnowflakeId();
var backoff = alg.ExponentialBackoffMs(2, 30_000);
```

## Complexity

- Consistent Hash: `O(1)`
- Rendezvous Hash: `O(n)`
- Snowflake ID: `O(1)`
- Vector Clock: `O(1)`
- Lamport Clock: `O(1)`
- Token Bucket: `O(1)`
- Sliding Window: `O(n)` worst
- Leaky Bucket: `O(1)`
- Exponential Backoff: `O(1)`
