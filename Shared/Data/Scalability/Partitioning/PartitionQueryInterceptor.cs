using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Usm.Shared.Data.Scalability.Partitioning;

/// <summary>
/// Diagnostic EF Core command interceptor that warns when a query targeting a registered
/// partitioned table does not include the partition-key column in its SQL text.
/// Such omissions cause PostgreSQL to scan all child partitions instead of pruning them,
/// which can dramatically increase I/O on large tables.
/// <para>
/// Enable in development/staging by calling <c>services.AddPartitionDiagnostics()</c> and
/// registering your tables via <see cref="RegisterPartitionedTable"/>.
/// </para>
/// </summary>
public sealed class PartitionQueryInterceptor(ILogger<PartitionQueryInterceptor> logger)
    : DbCommandInterceptor
{
    private readonly ILogger<PartitionQueryInterceptor> _logger = logger;
    private readonly Dictionary<string, string[]> _partitionColumns =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a partitioned table and its key columns for monitoring.
    /// Call during application startup after model creation.
    /// </summary>
    public void RegisterPartitionedTable(string tableName, params string[] keyColumns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        _partitionColumns[tableName] = keyColumns;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        InspectSql(command.CommandText);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        InspectSql(command.CommandText);
        return ValueTask.FromResult(result);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void InspectSql(string sql)
    {
        foreach (var (table, columns) in _partitionColumns)
        {
            if (!sql.Contains(table, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var column in columns)
            {
                if (!sql.Contains(column, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "[PartitionDiag] Query on partitioned table '{Table}' omits partition key '{Column}'. " +
                        "This may cause a full partition scan. SQL (truncated): {Sql}",
                        table, column,
                        sql.Length > 300 ? string.Concat(sql.AsSpan(0, 300), " …") : sql);
                }
            }
        }
    }
}
