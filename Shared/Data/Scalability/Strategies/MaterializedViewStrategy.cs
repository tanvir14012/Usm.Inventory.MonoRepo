using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Caching.Abstractions;
using Usm.Shared.Caching.Models;
using Usm.Shared.Data.Scalability.Abstractions;

namespace Usm.Shared.Data.Scalability.Strategies;

/// <summary>
/// Materialized-view + Redis caching strategy.
/// <list type="bullet">
/// <item>On reads: returns a Redis-cached snapshot when available (sub-millisecond).</item>
/// <item>Falls back to the materialized view via EF Core on a cache miss.</item>
/// <item>On writes: invalidates the snapshot so the next read re-queries the DB view.</item>
/// <item>Exposes <see cref="SyncToRedisAsync"/> so a background job can proactively prime the cache.</item>
/// </list>
/// </summary>
public sealed class MaterializedViewStrategy<TEntity>(
    ICacheService cacheService,
    IOptions<MaterializedViewOptions<TEntity>> options,
    ILogger<MaterializedViewStrategy<TEntity>> logger)
    : IDatabaseScalingStrategy<TEntity> where TEntity : class
{
    private readonly ICacheService _cacheService = cacheService;
    private readonly MaterializedViewOptions<TEntity> _options = options.Value;
    private readonly ILogger<MaterializedViewStrategy<TEntity>> _logger = logger;
    private readonly string _cacheKey = $"mv:{typeof(TEntity).Name}:snapshot";

    public ScalingStrategyType StrategyType => ScalingStrategyType.MaterializedView;
    public bool IsEnabled => _options.IsEnabled;

    public async ValueTask<IQueryable<TEntity>> ApplyReadStrategyAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        if (!_options.UseRedisCache)
            return query;

        try
        {
            var cached = await _cacheService
                .GetAsync<IReadOnlyList<TEntity>>(_cacheKey, cancellationToken)
                .ConfigureAwait(false);

            if (cached is { Count: > 0 })
            {
                _logger.LogDebug("MV cache HIT for {Entity} ({Count} rows).",
                    typeof(TEntity).Name, cached.Count);
                return cached.AsQueryable();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for MV snapshot — falling back to DB view.");
        }

        return query;
    }

    public async ValueTask ApplyWriteStrategyAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheService.RemoveAsync(_cacheKey, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate MV cache for {Entity}.", typeof(TEntity).Name);
        }
    }

    /// <summary>
    /// Writes a freshly-queried snapshot into Redis.
    /// Call from a scheduled background job to proactively refresh the materialized view cache.
    /// </summary>
    public async ValueTask SyncToRedisAsync(
        IReadOnlyList<TEntity> items,
        CancellationToken cancellationToken = default)
    {
        var entryOptions = new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.SnapshotTtlSeconds)
        };

        await _cacheService.SetAsync(_cacheKey, items, entryOptions, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "MV snapshot synced to Redis for {Entity} ({Count} rows, TTL={Ttl}s).",
            typeof(TEntity).Name, items.Count, _options.SnapshotTtlSeconds);
    }
}

// ── Options ───────────────────────────────────────────────────────────────────

public sealed class MaterializedViewOptions<TEntity> where TEntity : class
{
    public const string SectionName = "Database:MaterializedViews";
    public bool IsEnabled { get; set; } = true;
    public bool UseRedisCache { get; set; } = true;
    public int SnapshotTtlSeconds { get; set; } = 300;

    /// <summary>Override the DB view name (null = EF Core derives from entity table name).</summary>
    public string? ViewName { get; set; }
}
