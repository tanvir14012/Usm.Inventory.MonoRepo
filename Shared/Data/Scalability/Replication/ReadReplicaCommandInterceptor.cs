using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Usm.Shared.Data.Scalability.Replication;

/// <summary>
/// EF Core <see cref="DbCommandInterceptor"/> that transparently reroutes SELECT commands
/// to the configured read replica when <see cref="ReadReplicaContext.IsReadMode"/> is active.
/// <para>
/// Implementation notes:
/// <list type="bullet">
/// <item>Creates a short-lived <see cref="NpgsqlConnection"/> scoped to the reader's lifetime via
///   <see cref="CommandBehavior.CloseConnection"/>.</item>
/// <item>Copies all command parameters, preserving <see cref="NpgsqlParameter.NpgsqlDbType"/>.</item>
/// <item>Falls back silently to the primary on any replica failure.</item>
/// <item>Register as a scoped EF Core interceptor via <c>services.AddReadReplication()</c>.</item>
/// </list>
/// </para>
/// </summary>
public sealed class ReadReplicaCommandInterceptor(
    IOptions<ReplicationOptions> options,
    ILogger<ReadReplicaCommandInterceptor> logger) : DbCommandInterceptor
{
    private readonly ReplicationOptions _options = options.Value;
    private readonly ILogger<ReadReplicaCommandInterceptor> _logger = logger;

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (!ShouldRedirect())
            return result;

        try
        {
            var replicaConn = new NpgsqlConnection(_options.ReplicaConnectionString);
            await replicaConn.OpenAsync(cancellationToken).ConfigureAwait(false);

            var replicaCmd = BuildReplicaCommand(replicaConn, command);
            var reader = await replicaCmd
                .ExecuteReaderAsync(CommandBehavior.CloseConnection, cancellationToken)
                .ConfigureAwait(false);

            return InterceptionResult<DbDataReader>.SuppressWithResult(reader);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Read-replica execution failed — falling back to primary. Replica={Replica}",
                MaskConnectionString(_options.ReplicaConnectionString));
            return result;
        }
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        if (!ShouldRedirect())
            return result;

        try
        {
            var replicaConn = new NpgsqlConnection(_options.ReplicaConnectionString);
            replicaConn.Open();

            var replicaCmd = BuildReplicaCommand(replicaConn, command);
            var reader = replicaCmd.ExecuteReader(CommandBehavior.CloseConnection);
            return InterceptionResult<DbDataReader>.SuppressWithResult(reader);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Read-replica (sync) execution failed — falling back to primary.");
            return result;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool ShouldRedirect() =>
        _options.IsEnabled
        && !string.IsNullOrEmpty(_options.ReplicaConnectionString)
        && ReadReplicaContext.IsReadMode;

    private static NpgsqlCommand BuildReplicaCommand(NpgsqlConnection connection, DbCommand source)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = source.CommandText;
        cmd.CommandType = source.CommandType;
        cmd.CommandTimeout = source.CommandTimeout;

        foreach (DbParameter param in source.Parameters)
        {
            var copy = cmd.CreateParameter();
            copy.ParameterName = param.ParameterName;
            copy.Value = param.Value;
            copy.DbType = param.DbType;
            copy.Direction = param.Direction;
            copy.IsNullable = param.IsNullable;
            copy.Size = param.Size;

            // Preserve Npgsql-specific type info to avoid implicit cast overhead.
            if (param is NpgsqlParameter np && copy is NpgsqlParameter nc)
                nc.NpgsqlDbType = np.NpgsqlDbType;

            cmd.Parameters.Add(copy);
        }

        return cmd;
    }

    private static string MaskConnectionString(string cs)
    {
        var idx = cs.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
        return idx < 0 ? cs : string.Concat(cs.AsSpan(0, idx), "Password=***");
    }
}
