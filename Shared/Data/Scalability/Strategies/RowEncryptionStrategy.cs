using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Data.Scalability.Abstractions;

namespace Usm.Shared.Data.Scalability.Strategies;

/// <summary>
/// Row-level encryption strategy using AEAD (AES-256-GCM) or PostgreSQL <c>pgcrypto</c>.
/// <list type="bullet">
/// <item><b>Read path</b>: transparent — decryption is performed by EF Core value converters
///   registered on the entity's encrypted properties.</item>
/// <item><b>Write path</b>: marks the entity via <see cref="EncryptionContext"/> so that value
///   converters and SaveChanges interceptors can apply encryption before the INSERT/UPDATE.</item>
/// </list>
/// Configure encrypted columns by registering <c>EncryptedValueConverter</c> on each property
/// in your entity's <c>IEntityTypeConfiguration</c>.
/// </summary>
public sealed class RowEncryptionStrategy<TEntity>(
    IOptions<RowEncryptionOptions> options,
    ILogger<RowEncryptionStrategy<TEntity>> logger)
    : IDatabaseScalingStrategy<TEntity> where TEntity : class
{
    private readonly RowEncryptionOptions _options = options.Value;
    private readonly ILogger<RowEncryptionStrategy<TEntity>> _logger = logger;

    public ScalingStrategyType StrategyType => ScalingStrategyType.RowEncryption;
    public bool IsEnabled => _options.IsEnabled;

    // Decryption is handled transparently by EF Core value converters.
    public ValueTask<IQueryable<TEntity>> ApplyReadStrategyAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(query);

    public ValueTask ApplyWriteStrategyAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        EncryptionContext.MarkEntityForEncryption(entity, _options.Mode);

        _logger.LogDebug("Entity {Entity} marked for {Mode} encryption.",
            typeof(TEntity).Name, _options.Mode);

        return ValueTask.CompletedTask;
    }
}

// ── Supporting types ──────────────────────────────────────────────────────────

public enum EncryptionMode { AesGcm, PgCrypto }

public sealed class RowEncryptionOptions
{
    public const string SectionName = "Database:RowEncryption";
    public bool IsEnabled { get; set; } = false;
    public EncryptionMode Mode { get; set; } = EncryptionMode.AesGcm;

    /// <summary>Base64-encoded 256-bit key used when <see cref="Mode"/> is <see cref="EncryptionMode.AesGcm"/>.</summary>
    public string? EncryptionKeyBase64 { get; set; }

    /// <summary>Symmetric key used when <see cref="Mode"/> is <see cref="EncryptionMode.PgCrypto"/>.</summary>
    public string? PgCryptoKey { get; set; }
}

/// <summary>Ambient context tracking entities that must be encrypted before persistence.</summary>
public static class EncryptionContext
{
    private static readonly AsyncLocal<HashSet<object>?> _pendingEncryption = new();

    public static void MarkEntityForEncryption(object entity, EncryptionMode mode)
    {
        _pendingEncryption.Value ??= [];
        _pendingEncryption.Value.Add(entity);
    }

    public static bool IsMarkedForEncryption(object entity) =>
        _pendingEncryption.Value?.Contains(entity) ?? false;

    public static void Clear() => _pendingEncryption.Value?.Clear();
}
