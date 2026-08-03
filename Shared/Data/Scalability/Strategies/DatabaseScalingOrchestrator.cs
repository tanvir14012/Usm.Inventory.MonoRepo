using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Data.Scalability.Abstractions;
using Usm.Shared.Data.Scalability.Options;

namespace Usm.Shared.Data.Scalability.Strategies;

/// <summary>
/// Composes multiple <see cref="IDatabaseScalingStrategy{TEntity}"/> plug-ins into a single
/// pipeline. Strategies are applied in <see cref="ScalingStrategyType"/> enum-value order;
/// each is guarded by its own <see cref="IDatabaseScalingStrategy{TEntity}.IsEnabled"/> flag.
/// Register per entity type via <c>services.AddScalingOrchestratorFor&lt;TEntity&gt;()</c>.
/// </summary>
public sealed class DatabaseScalingOrchestrator<TEntity>(
    IEnumerable<IDatabaseScalingStrategy<TEntity>> strategies,
    IOptions<DatabaseScalingOptions> options,
    ILogger<DatabaseScalingOrchestrator<TEntity>> logger)
    where TEntity : class
{
    private readonly IDatabaseScalingStrategy<TEntity>[] _strategies =
        [.. strategies.OrderBy(static s => s.StrategyType)];
    private readonly DatabaseScalingOptions _options = options.Value;
    private readonly ILogger<DatabaseScalingOrchestrator<TEntity>> _logger = logger;

    /// <summary>Applies all enabled read-path strategies in pipeline order.</summary>
    public async ValueTask<IQueryable<TEntity>> ApplyReadAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        foreach (var strategy in _strategies)
        {
            if (!strategy.IsEnabled)
                continue;
            cancellationToken.ThrowIfCancellationRequested();

            if (_options.EnableDetailedLogging)
                _logger.LogDebug("Read strategy {Strategy} applying for {Entity}.",
                    strategy.StrategyType, typeof(TEntity).Name);

            query = await strategy.ApplyReadStrategyAsync(query, cancellationToken).ConfigureAwait(false);
        }

        return query;
    }

    /// <summary>Applies all enabled write-path strategies in pipeline order.</summary>
    public async ValueTask ApplyWriteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        foreach (var strategy in _strategies)
        {
            if (!strategy.IsEnabled)
                continue;
            cancellationToken.ThrowIfCancellationRequested();

            if (_options.EnableDetailedLogging)
                _logger.LogDebug("Write strategy {Strategy} applying for {Entity}.",
                    strategy.StrategyType, typeof(TEntity).Name);

            await strategy.ApplyWriteStrategyAsync(entity, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Returns <c>true</c> if the specified strategy type is active in this pipeline.</summary>
    public bool IsStrategyActive(ScalingStrategyType type) =>
        _strategies.Any(s => s.IsEnabled && s.StrategyType == type);
}
