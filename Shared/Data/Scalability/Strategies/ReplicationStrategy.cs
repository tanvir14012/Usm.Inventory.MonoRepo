using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Data.Scalability.Abstractions;
using Usm.Shared.Data.Scalability.Replication;

namespace Usm.Shared.Data.Scalability.Strategies;

/// <summary>
/// Enables read/write splitting by activating the read-replica ambient context on the read path.
/// The actual connection rerouting is performed by <see cref="ReadReplicaCommandInterceptor"/>
/// which is registered as a scoped EF Core interceptor.
/// </summary>
public sealed class ReplicationStrategy<TEntity>(
    IOptions<ReplicationOptions> options,
    ILogger<ReplicationStrategy<TEntity>> logger)
    : IDatabaseScalingStrategy<TEntity> where TEntity : class
{
    private readonly ReplicationOptions _options = options.Value;
    private readonly ILogger<ReplicationStrategy<TEntity>> _logger = logger;

    public ScalingStrategyType StrategyType => ScalingStrategyType.Replication;
    public bool IsEnabled => _options.IsEnabled && !string.IsNullOrEmpty(_options.ReplicaConnectionString);

    public ValueTask<IQueryable<TEntity>> ApplyReadStrategyAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        // The ambient flag tells ReadReplicaCommandInterceptor to redirect to the replica.
        // We return the same query — the interceptor handles the actual connection swap.
        if (ReadReplicaContext.IsReadMode)
            _logger.LogDebug("Replica routing already active for {Entity}.", typeof(TEntity).Name);

        return ValueTask.FromResult(query);
    }

    // Writes always go to the primary — nothing to do here.
    public ValueTask ApplyWriteStrategyAsync(TEntity entity, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
