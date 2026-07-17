using Iam.Application.Authorization;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Iam.Api.Endpoints.Organograms;

public sealed class GetOrganogramTemplateReferenceEndpoint : IEndpoint
{
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "en",
        "bn",
        "ar",
        "fr"
    };

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/organogram/templates/reference", Handle)
            .WithName("GetOrganogramTemplateReference")
            .WithTags("Organograms")
            .RequireAuthorization(PermissionPolicies.Permission(IamPermissions.OrganogramTemplateRead));
    }

    private static IResult Handle(HttpContext httpContext, IWebHostEnvironment environment)
    {
        var language = ResolveLanguage(httpContext.Request.Headers.AcceptLanguage.ToString());
        var referencePath = ResolveReferencePath(environment.ContentRootPath, language);

        if (!File.Exists(referencePath))
        {
            return Results.Problem(
                detail: "Localized organogram reference JSON file is not available.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Results.File(referencePath, "application/json");
    }

    private static string ResolveReferencePath(string contentRootPath, string language)
    {
        var baseDirectory = Path.Combine(contentRootPath, "ReferenceData", "Organograms");
        var localizedPath = Path.Combine(baseDirectory, $"us_military_organogram_template_{language}.json");

        if (File.Exists(localizedPath))
        {
            return localizedPath;
        }

        return Path.Combine(baseDirectory, "us_military_organogram_template_en.json");
    }

    private static string ResolveLanguage(string? acceptLanguageHeader)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguageHeader))
        {
            return "en";
        }

        foreach (var entry in acceptLanguageHeader.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var languageTag = entry.Split(';', 2)[0].Trim();
            if (string.IsNullOrWhiteSpace(languageTag))
            {
                continue;
            }

            var primaryLanguage = languageTag.Split('-', 2)[0];
            if (SupportedLanguages.Contains(primaryLanguage))
            {
                return primaryLanguage;
            }
        }

        return "en";
    }
}
