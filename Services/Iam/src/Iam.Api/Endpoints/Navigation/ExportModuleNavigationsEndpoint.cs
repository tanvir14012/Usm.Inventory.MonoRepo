using Iam.Application.Navigation.Queries;
using Iam.Domain.Navigation;
using MediatR;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Iam.Api.Endpoints.Navigation;

public sealed class ExportModuleNavigationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/navigation/modules/export", Handle)
            .WithName("ExportModuleNavigations")
            .WithTags("Navigation")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        MilitaryBuildingBlockType buildingBlockType,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var modules = await sender.Send(new GetModuleNavigationsQuery(buildingBlockType), cancellationToken);
        return Results.Ok(modules);
    }
}
