using MediatR;
using StoreHouse.Application.InventoryItems.Queries;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace StoreHouse.Api.Endpoints.InventoryItems;

public sealed class InventoryItemsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/inventory-items", Handle)
            .WithName("GetInventoryItems")
            .WithTags("InventoryItems")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(ISender sender, CancellationToken cancellationToken)
        => Results.Ok(await sender.Send(new GetInventoryItemsQuery(), cancellationToken));
}
