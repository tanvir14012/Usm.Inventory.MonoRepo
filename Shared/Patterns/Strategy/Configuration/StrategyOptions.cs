namespace Usm.Shared.Patterns.Strategy.Configuration;

/// <summary>
/// Configuration for strategy compilation and caching.
/// </summary>
public sealed class StrategyOptions
{
    /// <summary>Gets or sets a value indicating whether compiled expression delegates should be cached.</summary>
    public bool CacheCompiledExpressions { get; set; } = true;

    /// <summary>Gets or sets the maximum number of compiled delegates retained in memory.</summary>
    public int? CacheCapacity { get; set; }
}
