using Usm.Shared.Patterns.Cache;

namespace Usm.Shared.Patterns.Cache.Abstractions;

/// <summary>
/// Tracks cache usage and eviction behavior.
/// </summary>
public interface ICacheMetrics
{
    /// <summary>Gets the number of cache hits.</summary>
    long Hits { get; }

    /// <summary>Gets the number of cache misses.</summary>
    long Misses { get; }

    /// <summary>Gets the number of evictions.</summary>
    long Evictions { get; }

    /// <summary>Gets the number of expirations.</summary>
    long Expirations { get; }

    /// <summary>Gets a snapshot of the current metrics.</summary>
    CacheMetricsSnapshot Snapshot();
}
