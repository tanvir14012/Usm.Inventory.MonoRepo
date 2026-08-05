# Cache<TKey, TValue>

Reusable in-memory cache with LRU/LFU eviction, TTL expiration, metrics, and async-first access.

## Folder structure

```text
Shared/Patterns/Cache
├── Abstractions
├── Builders
├── Configuration
├── Extensions
├── Internal
├── Models
└── README.md
```

## Capabilities

- LRU and LFU eviction
- per-entry and default TTL
- expiration on access
- hit/miss/eviction/expiration metrics
- async-safe `GetOrCreateAsync`
- DI registration via `AddCacheFramework`

## Example

```csharp
var cache = Cache<string, CustomerDto>.CreateBuilder()
    .UseLru()
    .WithCapacity(10_000)
    .WithDefaultExpiration(TimeSpan.FromMinutes(10))
    .Build();

var customer = await cache.GetOrCreateAsync("customer:42", async token =>
{
    await Task.Delay(1, token);
    return await repo.GetAsync(42, token);
});
```

## Complexity

- Lookup: `O(1)` average
- Insert: `O(1)` average
- LRU eviction: `O(1)`
- LFU eviction: `O(log n)` for frequency bucket selection
- Expiration sweep: `O(n)` on write paths that touch expired entries
