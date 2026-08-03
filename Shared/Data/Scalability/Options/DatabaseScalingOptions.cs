using Usm.Shared.Data.Scalability.Abstractions;

namespace Usm.Shared.Data.Scalability.Options;

public sealed class DatabaseScalingOptions
{
    public const string SectionName = "Database:Scaling";

    /// <summary>
    /// Bitmask controlling which strategy types are globally active.
    /// Individual strategies also carry their own per-option <c>IsEnabled</c> flag;
    /// a strategy is only active when both flags agree.
    /// </summary>
    public ScalingStrategyType EnabledStrategies { get; set; } = ScalingStrategyType.None;

    /// <summary>
    /// When <c>true</c>, the <see cref="Strategies.DatabaseScalingOrchestrator{TEntity}"/>
    /// emits a debug log entry for each strategy application.
    /// Enable in development; disable in production to avoid log noise.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}
