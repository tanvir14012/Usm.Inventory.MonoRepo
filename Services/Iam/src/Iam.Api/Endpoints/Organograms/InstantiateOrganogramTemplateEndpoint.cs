using Iam.Application.Authorization;
using Iam.Application.Common;
using Iam.Application.Organograms.Commands;
using MediatR;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Iam.Api.Endpoints.Organograms;

public sealed class InstantiateOrganogramTemplateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/organogram/templates/{templateId:guid}/instantiate", Handle)
            .WithName("InstantiateOrganogramTemplate")
            .WithTags("Organograms")
            .RequireAuthorization(PermissionPolicies.Permission(IamPermissions.OrganogramTemplateInstantiate));
    }

    private static async Task<IResult> Handle(
        Guid templateId,
        InstantiateOrganogramTemplateRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new InstantiateOrganogramTemplateCommand(
                    templateId,
                    request.InstanceName,
                    request.InstanceCode,
                    request.OrganizationalUnits),
                cancellationToken);

            return Results.Ok(result);
        }
        catch (ApplicationValidationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }
}

public sealed record InstantiateOrganogramTemplateRequest(
    string InstanceName,
    string InstanceCode,
    IReadOnlyList<InstantiateOrganizationalUnitInput> OrganizationalUnits);
