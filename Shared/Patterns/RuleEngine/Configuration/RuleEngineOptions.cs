namespace Usm.Shared.Patterns.RuleEngine.Configuration;

/// <summary>
/// Configuration for the rule engine.
/// </summary>
public sealed class RuleEngineOptions
{
    /// <summary>Gets or sets a value indicating whether compiled rule expressions should be cached.</summary>
    public bool CacheCompiledExpressions { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the engine throws when no rule matches.</summary>
    public bool ThrowWhenNoRuleMatches { get; set; } = true;
}
