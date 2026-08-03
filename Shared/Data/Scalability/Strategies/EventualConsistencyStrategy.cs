using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Caching.Abstractions;
using Usm.Shared.Caching.Models;
using Usm.Shared.Data.Scalability.Abstractions;
using Usm.Shared.Data.Scalability.Replication;

namespace Usm.Shared.Data.Scalability.Strategies;

/// <summary>
/// Read-After-Write (RAW) eventual consistency strategy.
/// After each write, stores a short-lived flag in Redis keyed to the current async context.
/// The next read within the configured window then bypasses the read replica and queries
/// the primary — guaranteeing the calling context sees its own write.
/// </summary>
public sealed class EventualConsistencyStrategy<TEntity>(
    ICacheService cacheService,
    IOptions<EventualConsistencyOptions> options,
    ILogger<EventualConsistencyStrategy<TEntity>> logger)
    : IDatabaseScalingStrategy<TEntity> where TEntity : class
{
    private readonly ICacheService _cacheService = cacheService;
    private readonly EventualConsistencyOptions _options = options.Value;
    private readonly ILogger<EventualConsistencyStrategy<TEntity>> _logger = logger;

    public ScalingStrategyType StrategyType => ScalingStrategyType.EventualConsistency;
    public bool IsEnabled => _options.IsEnabled;

    public async ValueTask<IQueryable<TEntity>> ApplyReadStrategyAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var flag = await _cacheService
                .GetAsync<bool?>(BuildFlagKey(), cancellationToken)
                .ConfigureAwait(false);

            if (flag is true)
            {
                _logger.LogDebug(
                    "RAW flag active for {Entity} — forcing primary read to honour write visibility.",
                    typeof(TEntity).Name);
                ReadReplicaContext.ForceDisableForCurrentScope();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RAW flag check failed for {Entity}; proceeding normally.", typeof(TEntity).Name);
        }

        return query;
    }

    public async ValueTask ApplyWriteStrategyAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheService.SetAsync(
                BuildFlagKey(),
                true,
                new CacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.ReadAfterWriteWindowSeconds)
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set RAW flag for {Entity}.", typeof(TEntity).Name);
        }
    }

    /// <summary>
    /// Key is scoped to the entity type and current managed thread so that concurrent
    /// requests in different async contexts do not interfere with each other.
    /// </summary>
    private string BuildFlagKey() =>
        $"raw:{typeof(TEntity).Name}:{Environment.CurrentManagedThreadId}";
}

// ── Options ───────────────────────────────────────────────────────────────────

public sealed class EventualConsistencyOptions
{
    public const string SectionName = "Database:EventualConsistency";
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// How long (seconds) a Read-After-Write flag remains active after a write.
    /// Within this window, reads bypass the replica and go to the primary.
    /// </summary>
    public int ReadAfterWriteWindowSeconds { get; set; } = 30;
}
