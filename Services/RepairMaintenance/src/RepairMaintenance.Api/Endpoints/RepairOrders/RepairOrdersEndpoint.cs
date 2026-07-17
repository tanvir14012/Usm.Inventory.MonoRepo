using MediatR;
using RepairMaintenance.Application.RepairOrders.Queries;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace RepairMaintenance.Api.Endpoints.RepairOrders;

public sealed class RepairOrdersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/repair-orders", Handle)
            .WithName("GetRepairOrders")
            .WithTags("RepairOrders")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(ISender sender, CancellationToken cancellationToken)
        => Results.Ok(await sender.Send(new GetRepairOrdersQuery(), cancellationToken));
}
