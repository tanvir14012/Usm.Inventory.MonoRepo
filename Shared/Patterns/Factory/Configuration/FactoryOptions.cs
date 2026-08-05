namespace Usm.Shared.Patterns.Factory.Configuration;

/// <summary>
/// Configuration for expression compilation and caching.
/// </summary>
public sealed class FactoryOptions
{
    /// <summary>Gets or sets a value indicating whether compiled expression delegates should be cached.</summary>
    public bool CacheCompiledExpressions { get; set; } = true;

    /// <summary>Gets or sets the maximum number of compiled delegates to retain in memory.</summary>
    public int? CacheCapacity { get; set; }
}
