using Iam.Application.Authorization;
using Iam.Application.Organograms.Queries;
using MediatR;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Iam.Api.Endpoints.Organograms;

public sealed class GetOrganogramTemplatesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/organogram/templates", Handle)
            .WithName("GetOrganogramTemplates")
            .WithTags("Organograms")
            .RequireAuthorization(PermissionPolicies.Permission(IamPermissions.OrganogramTemplateRead));
    }

    private static async Task<IResult> Handle(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrganogramTemplatesQuery(), cancellationToken);
        return Results.Ok(result);
    }
}
