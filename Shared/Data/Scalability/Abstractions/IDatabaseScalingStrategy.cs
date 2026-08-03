namespace Usm.Shared.Data.Scalability.Abstractions;

/// <summary>Bitmask of available scaling strategy types.</summary>
[Flags]
public enum ScalingStrategyType
{
    None = 0,
    Replication = 1 << 0,
    Federation = 1 << 1,
    Sharding = 1 << 2,
    MaterializedView = 1 << 3,
    EventualConsistency = 1 << 4,
    RowEncryption = 1 << 5,
}

/// <summary>
/// Core scaling strategy contract. Each plug-in receives the query/entity and
/// transforms it according to its rules (routing, cache interception, encryption, etc.).
/// Multiple implementations are composed via <c>DatabaseScalingOrchestrator&lt;TEntity&gt;</c>.
/// </summary>
public interface IDatabaseScalingStrategy<TEntity> where TEntity : class
{
    ScalingStrategyType StrategyType { get; }
    bool IsEnabled { get; }

    /// <summary>Applies read-path transformations (replica routing, cache, MV selection, etc.).</summary>
    ValueTask<IQueryable<TEntity>> ApplyReadStrategyAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken = default);

    /// <summary>Applies write-path side effects (invalidation, shard routing, encryption marking, etc.).</summary>
    ValueTask ApplyWriteStrategyAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
}
