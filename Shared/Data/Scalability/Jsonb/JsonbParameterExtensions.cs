using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using NpgsqlTypes;

namespace Usm.Shared.Data.Scalability.Jsonb;

/// <summary>
/// EF Core <see cref="DbContext"/> extension methods for passing strongly-typed C# collections
/// to PostgreSQL functions and stored procedures as <c>jsonb</c> parameters.
/// <para>
/// All serialization is performed by <see cref="JsonbSerializer"/> using
/// <see cref="System.Text.Json"/> with minimal allocations.
/// </para>
/// </summary>
public static class JsonbParameterExtensions
{
    // ── Query functions (returns result set) ──────────────────────────────────

    /// <summary>
    /// Calls a PostgreSQL function that accepts a JSONB array and returns rows.
    /// <code>SELECT * FROM function_name(@p_jsonb)</code>
    /// </summary>
    public static async ValueTask<IReadOnlyList<TResult>> QueryJsonbFunctionAsync<TParam, TResult>(
        this DbContext context,
        string functionName,
        IReadOnlyList<TParam> parameters,
        CancellationToken cancellationToken = default)
        where TParam : class
        where TResult : class
    {
        var jsonb = JsonbSerializer.SerializeList(parameters);
        var param = new NpgsqlParameter("p_jsonb", NpgsqlDbType.Jsonb) { Value = jsonb };

        // EF1002: functionName is infrastructure-supplied (never user input) — suppressed intentionally.
#pragma warning disable EF1002
        return await context.Database
            .SqlQueryRaw<TResult>($"SELECT * FROM {functionName}(@p_jsonb)", param)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
#pragma warning restore EF1002
    }

    /// <summary>
    /// Calls a PostgreSQL function that accepts a single JSONB object and returns rows.
    /// </summary>
    public static async ValueTask<IReadOnlyList<TResult>> QueryJsonbFunctionAsync<TParam, TResult>(
        this DbContext context,
        string functionName,
        TParam parameter,
        CancellationToken cancellationToken = default)
        where TParam : class
        where TResult : class
    {
        var jsonb = JsonbSerializer.Serialize(parameter);
        var param = new NpgsqlParameter("p_jsonb", NpgsqlDbType.Jsonb) { Value = jsonb };

#pragma warning disable EF1002
        return await context.Database
            .SqlQueryRaw<TResult>($"SELECT * FROM {functionName}(@p_jsonb)", param)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
#pragma warning restore EF1002
    }

    // ── Procedures (no result set) ────────────────────────────────────────────

    /// <summary>
    /// Invokes a PostgreSQL stored procedure passing a JSONB array.
    /// <code>CALL procedure_name(@p_jsonb)</code>
    /// </summary>
    public static async ValueTask ExecuteJsonbProcedureAsync<TParam>(
        this DbContext context,
        string procedureName,
        IReadOnlyList<TParam> parameters,
        CancellationToken cancellationToken = default)
        where TParam : class
    {
        var jsonb = JsonbSerializer.SerializeList(parameters);
        var param = new NpgsqlParameter("p_jsonb", NpgsqlDbType.Jsonb) { Value = jsonb };

#pragma warning disable EF1002
        await context.Database
            .ExecuteSqlRawAsync($"CALL {procedureName}(@p_jsonb)", [param], cancellationToken)
            .ConfigureAwait(false);
#pragma warning restore EF1002
    }

    // ── Scalar function ────────────────────────────────────────────────────────

    /// <summary>
    /// Executes a scalar PostgreSQL function that accepts and returns a single JSONB value,
    /// then deserializes the result to <typeparamref name="TResult"/>.
    /// <code>SELECT function_name(@p_jsonb)</code>
    /// </summary>
    public static async ValueTask<TResult?> ExecuteJsonbScalarAsync<TParam, TResult>(
        this DbContext context,
        string functionName,
        TParam parameter,
        CancellationToken cancellationToken = default)
        where TParam : class
    {
        var jsonb = JsonbSerializer.Serialize(parameter);
        var conn = context.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT {functionName}(@p_jsonb)";
        var npgParam = new NpgsqlParameter("p_jsonb", NpgsqlDbType.Jsonb) { Value = jsonb };
        cmd.Parameters.Add(npgParam);

        var raw = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return raw is string s ? JsonbSerializer.Deserialize<TResult>(s) : default;
    }

    // ── Bulk / batch path ─────────────────────────────────────────────────────

    /// <summary>
    /// Passes a large array to a PostgreSQL function using the pooled
    /// <see cref="JsonbSerializer.SerializeArray{T}"/> path for minimal GC pressure.
    /// Ideal for bulk-import pipelines where <paramref name="items"/> is large.
    /// </summary>
    public static async ValueTask ExecuteJsonbBulkAsync<TParam>(
        this DbContext context,
        string functionName,
        ReadOnlyMemory<TParam> items,
        CancellationToken cancellationToken = default)
        where TParam : class
    {
        var jsonb = JsonbSerializer.SerializeArray(items.Span);
        var param = new NpgsqlParameter("p_jsonb", NpgsqlDbType.Jsonb) { Value = jsonb };

#pragma warning disable EF1002
        await context.Database
            .ExecuteSqlRawAsync($"SELECT {functionName}(@p_jsonb)", [param], cancellationToken)
            .ConfigureAwait(false);
#pragma warning restore EF1002
    }

    // ── Multi-parameter overload ───────────────────────────────────────────────

    /// <summary>
    /// Calls a PostgreSQL function with a JSONB array <b>and</b> additional scalar parameters.
    /// </summary>
    public static async ValueTask<IReadOnlyList<TResult>> QueryJsonbFunctionAsync<TParam, TResult>(
        this DbContext context,
        string functionName,
        IReadOnlyList<TParam> parameters,
        IReadOnlyDictionary<string, object?> extraParams,
        CancellationToken cancellationToken = default)
        where TParam : class
        where TResult : class
    {
        var jsonb = JsonbSerializer.SerializeList(parameters);
        var allParams = new List<object>(extraParams.Count + 1)
        {
            new NpgsqlParameter("p_jsonb", NpgsqlDbType.Jsonb) { Value = jsonb }
        };

        foreach (var (name, value) in extraParams)
            allParams.Add(new NpgsqlParameter(name, value ?? DBNull.Value));

        var paramPlaceholders = string.Join(", ",
            Enumerable.Range(0, allParams.Count).Select(i =>
                i == 0 ? "@p_jsonb" : $"@{((NpgsqlParameter)allParams[i]).ParameterName}"));

#pragma warning disable EF1002
        return await context.Database
            .SqlQueryRaw<TResult>($"SELECT * FROM {functionName}({paramPlaceholders})", [.. allParams])
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
#pragma warning restore EF1002
    }
}
