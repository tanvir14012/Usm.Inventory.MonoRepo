namespace Usm.Shared.Patterns.Pipeline.Configuration;

/// <summary>
/// Configuration for pipeline compilation and caching.
/// </summary>
public sealed class PipelineOptions
{
    /// <summary>Gets or sets a value indicating whether compiled expressions should be cached.</summary>
    public bool CacheCompiledExpressions { get; set; } = true;

    /// <summary>Gets or sets the maximum number of compiled delegates retained in memory.</summary>
    public int? CacheCapacity { get; set; }
}
