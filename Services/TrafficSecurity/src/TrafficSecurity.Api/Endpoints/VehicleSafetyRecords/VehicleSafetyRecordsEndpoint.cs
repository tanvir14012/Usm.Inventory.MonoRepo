using MediatR;
using TrafficSecurity.Application.VehicleSafetyRecords.Queries;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace TrafficSecurity.Api.Endpoints.VehicleSafetyRecords;

public sealed class VehicleSafetyRecordsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/vehicle-safety-records", Handle)
            .WithName("GetVehicleSafetyRecords")
            .WithTags("VehicleSafetyRecords")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(ISender sender, CancellationToken cancellationToken)
        => Results.Ok(await sender.Send(new GetVehicleSafetyRecordsQuery(), cancellationToken));
}
