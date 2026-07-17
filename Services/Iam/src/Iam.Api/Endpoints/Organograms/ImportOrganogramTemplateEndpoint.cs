using Iam.Application.Authorization;
using Iam.Application.Common;
using Iam.Application.Organograms.Commands;
using MediatR;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Iam.Api.Endpoints.Organograms;

public sealed class ImportOrganogramTemplateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/organogram/templates/import", Handle)
            .WithName("ImportOrganogramTemplate")
            .WithTags("Organograms")
            .RequireAuthorization(PermissionPolicies.Permission(IamPermissions.OrganogramTemplateImport));
    }

    private static async Task<IResult> Handle(
        string templateName,
        string templateVersion,
        IFormFile file,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return Results.BadRequest("Uploaded file is empty.");
        }

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);

        try
        {
            var response = await sender.Send(
                new ImportOrganogramTemplateCommand(templateName, templateVersion, ms.ToArray()),
                cancellationToken);
            return Results.Ok(response);
        }
        catch (ApplicationValidationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }
}
