using Microsoft.Extensions.Logging;
using Usm.Shared.Data.Scalability.Abstractions;
using Usm.Shared.Data.Scalability.Sharding;

namespace Usm.Shared.Data.Scalability.Strategies;

/// <summary>
/// Computes the shard key for each entity and surfaces it via <see cref="ShardContext"/>
/// so downstream infrastructure (connection factories, query rewriters) can route correctly.
/// </summary>
public sealed class ShardingStrategy<TEntity>(
    IShardRouter<TEntity> shardRouter,
    ILogger<ShardingStrategy<TEntity>> logger)
    : IDatabaseScalingStrategy<TEntity> where TEntity : class
{
    private readonly IShardRouter<TEntity> _shardRouter = shardRouter;
    private readonly ILogger<ShardingStrategy<TEntity>> _logger = logger;

    public ScalingStrategyType StrategyType => ScalingStrategyType.Sharding;
    public bool IsEnabled => true;

    // Read routing: propagate the ambient shard context if already set (e.g., from a prior write).
    public ValueTask<IQueryable<TEntity>> ApplyReadStrategyAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(query);

    public ValueTask ApplyWriteStrategyAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var shardKey = _shardRouter.GetShardKey(entity);
        var connection = _shardRouter.ResolveConnectionString(shardKey);
        ShardContext.SetCurrentShard(shardKey, connection);

        _logger.LogDebug("Entity {Entity} routed to shard key={Key}, index={Index}.",
            typeof(TEntity).Name, shardKey, _shardRouter.ResolveShardIndex(shardKey));

        return ValueTask.CompletedTask;
    }
}
