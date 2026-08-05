namespace Usm.Shared.Patterns.Cache;

/// <summary>
/// Immutable cache metrics snapshot.
/// </summary>
public sealed record CacheMetricsSnapshot(long Hits, long Misses, long Evictions, long Expirations);
