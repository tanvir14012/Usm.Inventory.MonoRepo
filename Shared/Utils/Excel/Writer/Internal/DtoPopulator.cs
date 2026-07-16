using System.Globalization;
using Usm.Shared.Utils.Excel.Writer.Abstractions;
using Usm.Shared.Utils.Excel.Writer.Models;

namespace Usm.Shared.Utils.Excel.Writer.Internal;

/// <summary>
/// Generic DTO populator backed by cached compiled setters.
/// Supports string, bool, all numeric types (byte/short/int/long/float/double/decimal),
/// DateTime, DateTimeOffset, TimeSpan, Guid, enums, and Nullable&lt;T&gt; variants of all of the above.
/// </summary>
public sealed class DtoPopulator<TDto> : IDtoPopulator<TDto> where TDto : new()
{
    // Descriptors are computed once per TDto type and shared across all instances.
    private static readonly PropertyDescriptor[] Descriptors =
        PropertyAccessorCache.GetDescriptors(typeof(TDto));

    public (TDto Dto, IReadOnlyList<PropertyConversionError> Errors) Populate(
        IReadOnlyList<string?> cellValues)
    {
        var dto    = new TDto();
        var errors = new List<PropertyConversionError>();

        foreach (var desc in Descriptors)
        {
            if (desc.ColumnIndex >= cellValues.Count)
                break; // no more data for remaining properties

            var raw = cellValues[desc.ColumnIndex];

            var (converted, errorMsg) = TryConvert(raw, desc);

            if (errorMsg is not null)
            {
                errors.Add(new PropertyConversionError(
                    desc.ColumnIndex,
                    desc.Property.Name,
                    raw,
                    FriendlyTypeName(desc.PropertyType),
                    errorMsg));
                continue;
            }

            // Only call the setter when we have a non-null value, or the property is nullable
            if (converted is not null || desc.IsNullable)
            {
                try
                {
                    desc.Setter(dto!, converted);
                }
                catch (Exception ex)
                {
                    errors.Add(new PropertyConversionError(
                        desc.ColumnIndex,
                        desc.Property.Name,
                        raw,
                        FriendlyTypeName(desc.PropertyType),
                        $"Setter threw: {ex.Message}"));
                }
            }
        }

        return (dto, errors);
    }

    // ── Conversion dispatch ───────────────────────────────────────────────────

    private static (object? Value, string? Error) TryConvert(string? raw, PropertyDescriptor desc)
    {
        var t = desc.UnderlyingType;

        // Empty cell: null for nullable, CLR default for value types
        if (string.IsNullOrEmpty(raw))
        {
            return desc.IsNullable
                ? (null, null)
                : (DefaultOf(t), null);
        }

        if (t == typeof(string))          return (raw, null);
        if (t == typeof(bool))            return ParseBool(raw);
        if (t == typeof(byte))            return ParseInteger<byte>(raw);
        if (t == typeof(sbyte))           return ParseInteger<sbyte>(raw);
        if (t == typeof(short))           return ParseInteger<short>(raw);
        if (t == typeof(ushort))          return ParseInteger<ushort>(raw);
        if (t == typeof(int))             return ParseInteger<int>(raw);
        if (t == typeof(uint))            return ParseInteger<uint>(raw);
        if (t == typeof(long))            return ParseInteger<long>(raw);
        if (t == typeof(ulong))           return ParseInteger<ulong>(raw);
        if (t == typeof(float))           return ParseSingle(raw);
        if (t == typeof(double))          return ParseDouble(raw);
        if (t == typeof(decimal))         return ParseDecimal(raw);
        if (t == typeof(Guid))            return ParseGuid(raw);
        if (t == typeof(DateTime))        return ParseDateTime(raw);
        if (t == typeof(DateTimeOffset))  return ParseDateTimeOffset(raw);
        if (t == typeof(TimeSpan))        return ParseTimeSpan(raw);
        if (t.IsEnum)                     return ParseEnum(raw, t);

        try
        {
            return (Convert.ChangeType(raw, t, CultureInfo.InvariantCulture), null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    // ── Parsers ───────────────────────────────────────────────────────────────

    private static (object?, string?) ParseBool(string raw)
    {
        if (bool.TryParse(raw, out var b))     return (b, null);
        if (raw is "1" or "yes" or "Yes")      return (true, null);
        if (raw is "0" or "no"  or "No")       return (false, null);
        return (null, $"Cannot convert '{raw}' to Boolean. Expected true/false/1/0.");
    }

    private delegate bool TryParseInt<T>(
        string s, NumberStyles style, IFormatProvider provider, out T result);

    private static (object?, string?) ParseInteger<T>(string raw) where T : struct
    {
        // Use Convert for integer types — handles hex, leading zeros, etc.
        try { return (Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture), null); }
        catch { return (null, $"Cannot convert '{raw}' to {typeof(T).Name}."); }
    }

    private static (object?, string?) ParseSingle(string raw)
        => float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            ? ((object?)v, null)
            : (null, $"Cannot convert '{raw}' to Single.");

    private static (object?, string?) ParseDouble(string raw)
        => double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            ? ((object?)v, null)
            : (null, $"Cannot convert '{raw}' to Double.");

    private static (object?, string?) ParseDecimal(string raw)
        => decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)
            ? ((object?)v, null)
            : (null, $"Cannot convert '{raw}' to Decimal.");

    private static (object?, string?) ParseGuid(string raw)
        => Guid.TryParse(raw, out var g)
            ? ((object?)g, null)
            : (null, $"Cannot convert '{raw}' to Guid.");

    private static (object?, string?) ParseDateTime(string raw)
    {
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return (dt, null);
        // Excel stores dates as OA (double) serial numbers
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial))
            return (DateTime.FromOADate(serial), null);
        return (null, $"Cannot convert '{raw}' to DateTime.");
    }

    private static (object?, string?) ParseDateTimeOffset(string raw)
        => DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var v)
            ? ((object?)v, null)
            : (null, $"Cannot convert '{raw}' to DateTimeOffset.");

    private static (object?, string?) ParseTimeSpan(string raw)
        => TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out var v)
            ? ((object?)v, null)
            : (null, $"Cannot convert '{raw}' to TimeSpan.");

    private static (object?, string?) ParseEnum(string raw, Type enumType)
        => Enum.TryParse(enumType, raw, ignoreCase: true, out var e)
            ? (e, null)
            : (null, $"'{raw}' is not a valid value for enum {enumType.Name}.");

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static object? DefaultOf(Type t)
        => t.IsValueType ? Activator.CreateInstance(t) : null;

    private static string FriendlyTypeName(Type t)
        => Nullable.GetUnderlyingType(t) is { } u ? $"{u.Name}?" : t.Name;
}
