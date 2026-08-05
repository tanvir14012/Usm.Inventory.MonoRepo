namespace Usm.Shared.Patterns.Cache.Configuration;

/// <summary>
/// Supported cache eviction policies.
/// </summary>
public enum CacheEvictionPolicy
{
    /// <summary>Evict the least recently used entry.</summary>
    Lru = 0,

    /// <summary>Evict the least frequently used entry.</summary>
    Lfu = 1
}

/// <summary>
/// Configuration for cache instances.
/// </summary>
public sealed class CacheOptions
{
    /// <summary>Gets or sets the eviction policy.</summary>
    public CacheEvictionPolicy Policy { get; set; } = CacheEvictionPolicy.Lru;

    /// <summary>Gets or sets the maximum number of entries.</summary>
    public int Capacity { get; set; } = 1024;

    /// <summary>Gets or sets the default expiration for new entries.</summary>
    public TimeSpan? DefaultExpiration { get; set; }
}

/// <summary>
/// Per-entry cache options.
/// </summary>
public sealed class CacheEntryOptions
{
    /// <summary>Gets or sets the absolute expiration relative to now.</summary>
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
}
