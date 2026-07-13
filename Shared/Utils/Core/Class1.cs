namespace Usm.Shared.Utils.Core;

public static class StringExtensions
{
    public static string NullIfWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public static bool EqualsInvariant(this string? value, string? other)
    {
        return string.Equals(value, other, StringComparison.OrdinalIgnoreCase);
    }
}
