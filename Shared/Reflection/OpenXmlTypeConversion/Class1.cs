namespace Usm.Shared.Reflection.OpenXmlTypeConversion;

public static class OpenXmlValueConverter
{
    public static object? ConvertCellText(string? cellText, Type targetType)
    {
        if (string.IsNullOrWhiteSpace(cellText))
        {
            return null;
        }

        var sourceType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (sourceType == typeof(string)) return cellText;
        if (sourceType == typeof(int) && int.TryParse(cellText, out var i)) return i;
        if (sourceType == typeof(long) && long.TryParse(cellText, out var l)) return l;
        if (sourceType == typeof(decimal) && decimal.TryParse(cellText, out var d)) return d;
        if (sourceType == typeof(bool) && bool.TryParse(cellText, out var b)) return b;
        if (sourceType == typeof(DateTime) && DateTime.TryParse(cellText, out var dt)) return dt;
        if (sourceType == typeof(Guid) && Guid.TryParse(cellText, out var g)) return g;
        if (sourceType.IsEnum && Enum.TryParse(sourceType, cellText, true, out var e)) return e;

        return Convert.ChangeType(cellText, sourceType);
    }
}
