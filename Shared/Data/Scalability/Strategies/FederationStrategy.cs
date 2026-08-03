using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Data.Scalability.Abstractions;

namespace Usm.Shared.Data.Scalability.Strategies;

/// <summary>
/// Federation (bounded-context DB routing) strategy.
/// Routes reads and writes for a specific entity to the database configured for that
/// entity type in <see cref="FederationOptions.EntityConnectionMap"/>.
/// The resolved connection string is surfaced via <see cref="FederationContext"/>
/// for use by connection factories or EF Core connection interceptors.
/// </summary>
public sealed class FederationStrategy<TEntity>(
    IOptions<FederationOptions> options,
    ILogger<FederationStrategy<TEntity>> logger)
    : IDatabaseScalingStrategy<TEntity> where TEntity : class
{
    private readonly FederationOptions _options = options.Value;
    private readonly ILogger<FederationStrategy<TEntity>> _logger = logger;
    private readonly string _entityTypeName = typeof(TEntity).Name;

    public ScalingStrategyType StrategyType => ScalingStrategyType.Federation;
    public bool IsEnabled => _options.IsEnabled &&
                             _options.EntityConnectionMap.ContainsKey(_entityTypeName);

    public ValueTask<IQueryable<TEntity>> ApplyReadStrategyAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        SetConnectionContext();
        return ValueTask.FromResult(query);
    }

    public ValueTask ApplyWriteStrategyAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        SetConnectionContext();
        return ValueTask.CompletedTask;
    }

    private void SetConnectionContext()
    {
        if (_options.EntityConnectionMap.TryGetValue(_entityTypeName, out var conn))
        {
            FederationContext.SetCurrentConnection(conn);
            _logger.LogDebug("Federation: routing {Entity} to dedicated database.", _entityTypeName);
        }
    }
}

// ── Options & ambient context ─────────────────────────────────────────────────

public sealed class FederationOptions
{
    public const string SectionName = "Database:Federation";
    public bool IsEnabled { get; set; } = true;

    /// <summary>Maps CLR entity type name → dedicated Npgsql connection string.</summary>
    public Dictionary<string, string> EntityConnectionMap { get; set; } = [];
}

/// <summary>Ambient context carrying the federation connection string for the current async flow.</summary>
public static class FederationContext
{
    private static readonly AsyncLocal<string?> _connectionString = new();

    public static string? CurrentConnectionString => _connectionString.Value;

    public static void SetCurrentConnection(string connectionString) =>
        _connectionString.Value = connectionString;

    public static void Clear() => _connectionString.Value = null;
}
