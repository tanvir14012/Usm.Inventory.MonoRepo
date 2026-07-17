using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Usm.Shared.Contracts.Localization;

namespace Usm.Shared.Data.DbContextExtensions;

public static class LocalizationHeaderExtensions
{
    private static readonly ISet<string> SupportedLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "en",
        "zh",
        "hi",
        "es",
        "fr",
        "ar",
        "bn",
        "pt",
        "ru"
    };

    public static string ResolveLanguageFromContentType(string? contentTypeHeader)
    {
        if (string.IsNullOrWhiteSpace(contentTypeHeader))
            return "en";

        var segments = contentTypeHeader.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            var langToken = "lang=";
            if (!segment.StartsWith(langToken, StringComparison.OrdinalIgnoreCase))
                continue;

            var value = segment[langToken.Length..].Trim().Trim('"', '\'');
            return NormalizeLanguage(value);
        }

        return "en";
    }

    public static string ResolveLanguageFromAcceptLanguage(string? acceptLanguageHeader)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguageHeader))
            return "en";

        var cultureToken = acceptLanguageHeader.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
            .FirstOrDefault(part => !string.IsNullOrWhiteSpace(part));

        return NormalizeLanguage(cultureToken);
    }

    public static LocalizedText UpsertByContentTypeLanguage(
        this LocalizedText? currentValue,
        string? contentTypeHeader,
        string? value)
    {
        var language = ResolveLanguageFromContentType(contentTypeHeader);
        var normalizedValue = value?.Trim() ?? string.Empty;
        var current = currentValue ?? LocalizedText.Empty;

        return language switch
        {
            "zh" => current with { Zh = normalizedValue },
            "hi" => current with { Hi = normalizedValue },
            "es" => current with { Es = normalizedValue },
            "fr" => current with { Fr = normalizedValue },
            "ar" => current with { Ar = normalizedValue },
            "bn" => current with { Bn = normalizedValue },
            "pt" => current with { Pt = normalizedValue },
            "ru" => current with { Ru = normalizedValue },
            _ => current with { En = normalizedValue }
        };
    }

    internal static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return "en";

        try
        {
            var culture = CultureInfo.GetCultureInfo(language);
            var isoCode = culture.TwoLetterISOLanguageName;
            return SupportedLanguages.Contains(isoCode) ? isoCode : "en";
        }
        catch (CultureNotFoundException)
        {
            var trimmed = language.Trim();
            var matched = SupportedLanguages.FirstOrDefault(code =>
                trimmed.StartsWith(code, StringComparison.OrdinalIgnoreCase));
            return matched ?? "en";
        }
    }
}

public static class PostgreSqlJsonDbFunctions
{
    [DbFunction("jsonb_extract_path_text", IsBuiltIn = true)]
    public static string? JsonbExtractPathText(object? json, string path)
        => throw new NotSupportedException("This method is for SQL translation only.");
}

public static class LocalizedJsonQueryExtensions
{
    public static IQueryable<string?> SelectLocalizedJsonValue<TEntity>(
        this IQueryable<TEntity> source,
        string jsonColumnName,
        string? languageHeader)
        where TEntity : class
    {
        var language = LocalizationHeaderExtensions.NormalizeLanguage(languageHeader);
        return source.Select(entity =>
            PostgreSqlJsonDbFunctions.JsonbExtractPathText(
                EF.Property<object?>(entity, jsonColumnName),
                language));
    }

    public static IQueryable<TEntity> WhereLocalizedJsonContains<TEntity>(
        this IQueryable<TEntity> source,
        string jsonColumnName,
        string? searchTerm,
        string? acceptLanguageHeader)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return source;

        var language = LocalizationHeaderExtensions.ResolveLanguageFromAcceptLanguage(acceptLanguageHeader);
        var escaped = EscapeLikePattern(searchTerm.Trim());
        var pattern = $"%{escaped}%";

        return source.Where(entity =>
            NpgsqlDbFunctionsExtensions.ILike(
                EF.Functions,
                PostgreSqlJsonDbFunctions.JsonbExtractPathText(EF.Property<object?>(entity, jsonColumnName), language) ?? string.Empty,
                pattern));
    }

    internal static string EscapeLikePattern(string value)
        => value.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
}
