namespace Usm.Shared.Reflection.ClrEfTypeMapping;

public static class ClrToDbTypeMap
{
    public static string ToPostgresType(Type type)
    {
        var source = Nullable.GetUnderlyingType(type) ?? type;

        if (source == typeof(string)) return "text";
        if (source == typeof(int)) return "integer";
        if (source == typeof(long)) return "bigint";
        if (source == typeof(short)) return "smallint";
        if (source == typeof(decimal)) return "numeric";
        if (source == typeof(double)) return "double precision";
        if (source == typeof(float)) return "real";
        if (source == typeof(bool)) return "boolean";
        if (source == typeof(DateTime) || source == typeof(DateTimeOffset)) return "timestamp with time zone";
        if (source == typeof(Guid)) return "uuid";
        if (source.IsEnum) return "integer";

        return "jsonb";
    }
}
