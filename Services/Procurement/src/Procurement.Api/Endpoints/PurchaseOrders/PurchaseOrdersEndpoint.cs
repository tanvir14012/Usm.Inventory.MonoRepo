using MediatR;
using Procurement.Application.PurchaseOrders.Queries;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Procurement.Api.Endpoints.PurchaseOrders;

public sealed class PurchaseOrdersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/purchase-orders", Handle)
            .WithName("GetPurchaseOrders")
            .WithTags("PurchaseOrders")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(ISender sender, CancellationToken cancellationToken)
        => Results.Ok(await sender.Send(new GetPurchaseOrdersQuery(), cancellationToken));
}
