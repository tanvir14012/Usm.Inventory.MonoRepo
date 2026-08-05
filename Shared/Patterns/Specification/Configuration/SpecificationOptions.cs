namespace Usm.Shared.Patterns.Specification.Configuration;

/// <summary>
/// Configuration for specification compilation and caching.
/// </summary>
public sealed class SpecificationOptions
{
    /// <summary>Gets or sets a value indicating whether compiled expression delegates should be cached.</summary>
    public bool CacheCompiledExpressions { get; set; } = true;

    /// <summary>Gets or sets the maximum number of compiled expression delegates to retain in cache.</summary>
    public int? CacheCapacity { get; set; }
}
