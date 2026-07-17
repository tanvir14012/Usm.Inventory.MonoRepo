using Iam.Application.Navigation.Queries;
using Iam.Domain.Navigation;
using MediatR;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Iam.Api.Endpoints.Navigation;

public sealed class GetModuleNavigationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/navigation/modules", Handle)
            .WithName("GetModuleNavigations")
            .WithTags("Navigation")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        MilitaryBuildingBlockType? buildingBlockType,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetModuleNavigationsQuery(buildingBlockType), cancellationToken);
        return Results.Ok(result);
    }
}
