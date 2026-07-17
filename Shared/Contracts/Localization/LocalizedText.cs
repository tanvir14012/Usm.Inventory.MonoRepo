namespace Usm.Shared.Contracts.Localization;

public sealed record LocalizedText(
    string En = "",
    string Zh = "",
    string Hi = "",
    string Es = "",
    string Fr = "",
    string Ar = "",
    string Bn = "",
    string Pt = "",
    string Ru = "")
{
    public static LocalizedText Empty { get; } = new();

    public string ForCulture(string? cultureCode)
    {
        var normalized = cultureCode?.Trim().ToLowerInvariant() ?? "en";

        return normalized switch
        {
            var c when c.StartsWith("zh", StringComparison.Ordinal) => string.IsNullOrWhiteSpace(Zh) ? En : Zh,
            var c when c.StartsWith("hi", StringComparison.Ordinal) => string.IsNullOrWhiteSpace(Hi) ? En : Hi,
            var c when c.StartsWith("es", StringComparison.Ordinal) => string.IsNullOrWhiteSpace(Es) ? En : Es,
            var c when c.StartsWith("fr", StringComparison.Ordinal) => string.IsNullOrWhiteSpace(Fr) ? En : Fr,
            var c when c.StartsWith("ar", StringComparison.Ordinal) => string.IsNullOrWhiteSpace(Ar) ? En : Ar,
            var c when c.StartsWith("bn", StringComparison.Ordinal) => string.IsNullOrWhiteSpace(Bn) ? En : Bn,
            var c when c.StartsWith("pt", StringComparison.Ordinal) => string.IsNullOrWhiteSpace(Pt) ? En : Pt,
            var c when c.StartsWith("ru", StringComparison.Ordinal) => string.IsNullOrWhiteSpace(Ru) ? En : Ru,
            _ => En
        };
    }
}
