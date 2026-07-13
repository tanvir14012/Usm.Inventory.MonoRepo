namespace Usm.Shared.Contracts.Localization;

public sealed record LocalizedText(string En = "", string Fr = "", string Ar = "")
{
    public static LocalizedText Empty { get; } = new();

    public string ForCulture(string? cultureCode)
    {
        var normalized = cultureCode?.Trim().ToLowerInvariant() ?? "en";
        return normalized switch
        {
            var c when c.StartsWith("fr") => string.IsNullOrWhiteSpace(Fr) ? En : Fr,
            var c when c.StartsWith("ar") => string.IsNullOrWhiteSpace(Ar) ? En : Ar,
            _ => En
        };
    }
}
