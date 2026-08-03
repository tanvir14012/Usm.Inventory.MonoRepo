using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Usm.Shared.Data.Scalability.Abstractions;

namespace Usm.Shared.Data.Scalability.ScriptSeeding;

/// <summary>
/// High-performance, idempotent SQL script execution engine backed by Npgsql.
/// <list type="bullet">
/// <item>Scans <see cref="ScriptSeedingOptions.ScriptDirectory"/> recursively for <c>.sql</c> files.</item>
/// <item>Orders scripts by folder precedence (<see cref="ScriptSeedingOptions.ScriptFolderOrder"/>)
///      then by the numeric prefix in each filename (e.g., <c>001_create_types.sql</c> → order 1).</item>
/// <item>Tracks applied scripts in <c>__script_migrations</c> using a SHA-256 checksum.</item>
/// <item>Re-applies a script only when its checksum changes (content-addressable idempotency).</item>
/// <item>Wraps each execution in a transaction (configurable via <see cref="ScriptSeedingOptions.UseTransactionPerScript"/>).</item>
/// </list>
/// Supported DB object types: table seeds, functions, stored procedures, views,
/// materialized views, and pg_cron / background-worker scheduler scripts.
/// </summary>
public sealed class ScriptMigrationEngine(
    IOptions<ScriptSeedingOptions> options,
    ILogger<ScriptMigrationEngine> logger) : IScriptMigrationEngine
{
    private readonly ScriptSeedingOptions _options = options.Value;
    private readonly ILogger<ScriptMigrationEngine> _logger = logger;

    private const string CreateTrackingTableSql = """
        CREATE TABLE IF NOT EXISTS {0}.{1} (
            script_name  TEXT        NOT NULL PRIMARY KEY,
            applied_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            checksum     TEXT        NOT NULL,
            duration_ms  INT         NOT NULL DEFAULT 0
        );
        """;

    // ── IScriptMigrationEngine ────────────────────────────────────────────────

    public async ValueTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            throw new InvalidOperationException(
                $"'{nameof(ScriptSeedingOptions.ConnectionString)}' must be configured for script seeding.");

        var directory = ResolveDirectory();
        if (!Directory.Exists(directory))
        {
            _logger.LogWarning("Script directory '{Dir}' does not exist — skipping seeding.", directory);
            return;
        }

        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await EnsureTrackingTableAsync(connection, cancellationToken).ConfigureAwait(false);
        var applied = await GetAppliedChecksumMapAsync(connection, cancellationToken).ConfigureAwait(false);

        var pending = GetOrderedScripts(directory)
            .Where(s => !applied.TryGetValue(s.Name, out var cs) || cs != s.Checksum)
            .ToList();

        if (pending.Count == 0)
        {
            _logger.LogInformation("Script seeding: all scripts are up-to-date.");
            return;
        }

        _logger.LogInformation("Script seeding: {Count} script(s) pending.", pending.Count);

        foreach (var script in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ApplyScriptAsync(connection, script, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Script seeding completed.");
    }

    public async ValueTask<IReadOnlyList<string>> GetPendingScriptsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnsureTrackingTableAsync(connection, cancellationToken).ConfigureAwait(false);

        var applied = await GetAppliedChecksumMapAsync(connection, cancellationToken).ConfigureAwait(false);
        var directory = ResolveDirectory();
        if (!Directory.Exists(directory)) return [];

        return GetOrderedScripts(directory)
            .Where(s => !applied.TryGetValue(s.Name, out var cs) || cs != s.Checksum)
            .Select(static s => s.Name)
            .ToList();
    }

    public async ValueTask<IReadOnlyList<string>> GetAppliedScriptsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnsureTrackingTableAsync(connection, cancellationToken).ConfigureAwait(false);

        var applied = await GetAppliedChecksumMapAsync(connection, cancellationToken).ConfigureAwait(false);
        return [.. applied.Keys];
    }

    // ── Private implementation ────────────────────────────────────────────────

    private string ResolveDirectory() =>
        Path.IsPathRooted(_options.ScriptDirectory)
            ? _options.ScriptDirectory
            : Path.GetFullPath(_options.ScriptDirectory, AppContext.BaseDirectory);

    private async Task EnsureTrackingTableAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var ddl = string.Format(CreateTrackingTableSql, _options.TrackingSchema, _options.TrackingTable);
        await using var cmd = new NpgsqlCommand(ddl, conn) { CommandTimeout = 30 };
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private async Task<Dictionary<string, string>> GetAppliedChecksumMapAsync(
        NpgsqlConnection conn, CancellationToken ct)
    {
        var sql = $"SELECT script_name, checksum FROM {_options.TrackingSchema}.{_options.TrackingTable}";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            result[reader.GetString(0)] = reader.GetString(1);

        return result;
    }

    private async Task ApplyScriptAsync(
        NpgsqlConnection connection, ScriptRecord script, CancellationToken ct)
    {
        _logger.LogInformation("Applying script: {Script}", script.Name);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (_options.UseTransactionPerScript)
            {
                await using var tx = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
                await ExecuteSqlAsync(connection, script.Content, ct).ConfigureAwait(false);
                await RecordScriptAsync(connection, script, (int)sw.ElapsedMilliseconds, ct).ConfigureAwait(false);
                await tx.CommitAsync(ct).ConfigureAwait(false);
            }
            else
            {
                await ExecuteSqlAsync(connection, script.Content, ct).ConfigureAwait(false);
                await RecordScriptAsync(connection, script, (int)sw.ElapsedMilliseconds, ct).ConfigureAwait(false);
            }

            _logger.LogInformation("Script '{Script}' applied in {Ms}ms.", script.Name, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply script '{Script}'.", script.Name);
            throw;
        }
    }

    private async Task ExecuteSqlAsync(NpgsqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(sql, conn)
        {
            CommandTimeout = _options.CommandTimeoutSeconds
        };
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private async Task RecordScriptAsync(
        NpgsqlConnection conn, ScriptRecord script, int durationMs, CancellationToken ct)
    {
        var upsert = $"""
            INSERT INTO {_options.TrackingSchema}.{_options.TrackingTable}
                (script_name, applied_at, checksum, duration_ms)
            VALUES (@name, NOW(), @checksum, @duration)
            ON CONFLICT (script_name) DO UPDATE
                SET applied_at  = EXCLUDED.applied_at,
                    checksum    = EXCLUDED.checksum,
                    duration_ms = EXCLUDED.duration_ms;
            """;

        await using var cmd = new NpgsqlCommand(upsert, conn);
        cmd.Parameters.AddWithValue("name",     script.Name);
        cmd.Parameters.AddWithValue("checksum", script.Checksum);
        cmd.Parameters.AddWithValue("duration", durationMs);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private IReadOnlyList<ScriptRecord> GetOrderedScripts(string rootDirectory)
    {
        var folderPriority = _options.ScriptFolderOrder
            .Select(static (f, i) => (f, i))
            .ToDictionary(static x => x.f, static x => x.i, StringComparer.OrdinalIgnoreCase);

        var records = new List<ScriptRecord>();

        foreach (var filePath in Directory.EnumerateFiles(rootDirectory, "*.sql", SearchOption.AllDirectories))
        {
            var relativeName = Path.GetRelativePath(rootDirectory, filePath).Replace('\\', '/');
            var content = File.ReadAllText(filePath, Encoding.UTF8);
            var checksum = ComputeChecksum(content);

            // Derive execution order from folder + numeric filename prefix.
            var firstSlash = relativeName.IndexOf('/');
            var folder = firstSlash > 0 ? relativeName[..firstSlash] : string.Empty;
            var fileName = Path.GetFileName(filePath);

            var folderOrder = folderPriority.TryGetValue(folder, out var fo) ? fo : 999;
            var fileOrder = ExtractNumericPrefix(fileName);

            records.Add(new ScriptRecord(
                relativeName,
                filePath,
                content,
                checksum,
                folderOrder * 10_000 + fileOrder));
        }

        records.Sort(static (a, b) => a.Order.CompareTo(b.Order));
        return records;
    }

    private static int ExtractNumericPrefix(string fileName)
    {
        var sb = new StringBuilder(8);
        foreach (var c in fileName)
        {
            if (!char.IsDigit(c)) break;
            sb.Append(c);
        }
        return sb.Length > 0 && int.TryParse(sb.ToString(), out var n) ? n : int.MaxValue;
    }

    private static string ComputeChecksum(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash  = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
