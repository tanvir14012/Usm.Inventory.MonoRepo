using MediatR;
using TrafficSecurity.Domain.VehicleSafetyRecords;

namespace TrafficSecurity.Application.VehicleSafetyRecords.Queries;

public record VehicleSafetyRecordDto(Guid Id, Guid VehicleId, SafetyStatus Status, DateTimeOffset InspectionDate);

public record GetVehicleSafetyRecordsQuery : IRequest<IReadOnlyList<VehicleSafetyRecordDto>>;

public class GetVehicleSafetyRecordsQueryHandler
    : IRequestHandler<GetVehicleSafetyRecordsQuery, IReadOnlyList<VehicleSafetyRecordDto>>
{
    public Task<IReadOnlyList<VehicleSafetyRecordDto>> Handle(
        GetVehicleSafetyRecordsQuery request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<VehicleSafetyRecordDto>>(Array.Empty<VehicleSafetyRecordDto>());
    }
}
