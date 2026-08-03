using Microsoft.EntityFrameworkCore;

namespace Usm.Shared.Data.Scalability.Functions;

/// <summary>
/// Stub methods for PostgreSQL built-in functions that can be called inside EF Core
/// LINQ queries. All methods throw <see cref="InvalidOperationException"/> when invoked
/// client-side — they are evaluated exclusively on the PostgreSQL server.
/// <para>
/// Register them by calling <c>modelBuilder.RegisterPostgreSqlBuiltInFunctions()</c>
/// inside your <c>OnModelCreating</c> override.
/// </para>
/// </summary>
public static class PostgreSqlDbFunctions
{
    private static T ServerSideOnly<T>(string name) =>
        throw new InvalidOperationException(
            $"'{name}' is a server-side PostgreSQL function. " +
            "Call it only inside an EF Core LINQ query expression.");

    // ── JSONB ─────────────────────────────────────────────────────────────────

    /// <summary>Returns the number of elements in a top-level JSON array.</summary>
    [DbFunction("jsonb_array_length", IsBuiltIn = true)]
    public static int JsonbArrayLength(string jsonb)
        => ServerSideOnly<int>("jsonb_array_length");

    /// <summary>Returns <c>true</c> if a SQL/JSON path exists in the JSONB value.</summary>
    [DbFunction("jsonb_path_exists", IsBuiltIn = true)]
    public static bool JsonbPathExists(string jsonb, string jsonPath)
        => ServerSideOnly<bool>("jsonb_path_exists");

    /// <summary>Tests whether a SQL/JSON path predicate holds for the JSONB value.</summary>
    [DbFunction("jsonb_path_match", IsBuiltIn = true)]
    public static bool JsonbPathMatch(string jsonb, string jsonPath)
        => ServerSideOnly<bool>("jsonb_path_match");

    /// <summary>Returns the JSON type of the outermost value ("object", "array", "string", etc.).</summary>
    [DbFunction("jsonb_typeof", IsBuiltIn = true)]
    public static string JsonbTypeOf(string jsonb)
        => ServerSideOnly<string>("jsonb_typeof");

    /// <summary>Recursively removes all object fields that have null values from a JSONB value.</summary>
    [DbFunction("jsonb_strip_nulls", IsBuiltIn = true)]
    public static string JsonbStripNulls(string jsonb)
        => ServerSideOnly<string>("jsonb_strip_nulls");

    /// <summary>Returns a JSONB value as a formatted string (pretty-printed).</summary>
    [DbFunction("jsonb_pretty", IsBuiltIn = true)]
    public static string JsonbPretty(string jsonb)
        => ServerSideOnly<string>("jsonb_pretty");

    // ── Full-text search ──────────────────────────────────────────────────────

    /// <summary>Computes the rank of a tsvector document against a tsquery.</summary>
    [DbFunction("ts_rank", IsBuiltIn = true)]
    public static float TsRank(NpgsqlTypes.NpgsqlTsVector vector, NpgsqlTypes.NpgsqlTsQuery query)
        => ServerSideOnly<float>("ts_rank");

    /// <summary>Computes a rank normalised against document length.</summary>
    [DbFunction("ts_rank_cd", IsBuiltIn = true)]
    public static float TsRankCd(NpgsqlTypes.NpgsqlTsVector vector, NpgsqlTypes.NpgsqlTsQuery query)
        => ServerSideOnly<float>("ts_rank_cd");

    // ── String / pattern matching (pg_trgm) ──────────────────────────────────

    /// <summary>Returns the trigram similarity score between two strings (requires pg_trgm).</summary>
    [DbFunction("similarity", IsBuiltIn = true)]
    public static float Similarity(string a, string b)
        => ServerSideOnly<float>("similarity");

    /// <summary>Returns the word-level similarity between two strings (requires pg_trgm).</summary>
    [DbFunction("word_similarity", IsBuiltIn = true)]
    public static float WordSimilarity(string a, string b)
        => ServerSideOnly<float>("word_similarity");

    /// <summary>Replaces the first (or all, when flags include 'g') regex matches in a string.</summary>
    [DbFunction("regexp_replace", IsBuiltIn = true)]
    public static string RegexpReplace(string source, string pattern, string replacement)
        => ServerSideOnly<string>("regexp_replace");

    /// <summary>Replaces regex matches with flags (e.g., 'g' for global, 'i' for case-insensitive).</summary>
    [DbFunction("regexp_replace", IsBuiltIn = true)]
    public static string RegexpReplace(string source, string pattern, string replacement, string flags)
        => ServerSideOnly<string>("regexp_replace");

    // ── Date / time ───────────────────────────────────────────────────────────

    /// <summary>Truncates a timestamp to the specified precision (e.g., "month", "day", "hour").</summary>
    [DbFunction("date_trunc", IsBuiltIn = true)]
    public static DateTime DateTrunc(string field, DateTime value)
        => ServerSideOnly<DateTime>("date_trunc");

    /// <summary>Returns the specified component of a date/time (e.g., "year", "month", "epoch").</summary>
    [DbFunction("date_part", IsBuiltIn = true)]
    public static double DatePart(string field, DateTime value)
        => ServerSideOnly<double>("date_part");

    // ── Array ─────────────────────────────────────────────────────────────────

    /// <summary>Returns the length of the array dimension (1-based).</summary>
    [DbFunction("array_length", IsBuiltIn = true)]
    public static int ArrayLength(object array, int dimension)
        => ServerSideOnly<int>("array_length");

    /// <summary>Returns the index of the first occurrence of the element in the array (1-based), or null.</summary>
    [DbFunction("array_position", IsBuiltIn = true)]
    public static int? ArrayPosition(object array, object element)
        => ServerSideOnly<int?>("array_position");

    // ── Null handling ─────────────────────────────────────────────────────────

    /// <summary>Returns null when <paramref name="value"/> equals <paramref name="nullValue"/>.</summary>
    [DbFunction("nullif", IsBuiltIn = true)]
    public static T? NullIf<T>(T value, T nullValue)
        => ServerSideOnly<T?>("nullif");
}
