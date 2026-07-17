using Microsoft.EntityFrameworkCore;
using TrafficSecurity.Application.Abstractions;
using MediatR;
using TrafficSecurity.Domain.VehicleSafetyRecords;

namespace TrafficSecurity.Application.VehicleSafetyRecords.Queries;

public record VehicleSafetyRecordDto(
    Guid Id,
    string VehicleRegistrationNumber,
    Guid VehicleId,
    SafetyStatus Status,
    DateTimeOffset InspectionDate,
    DateTimeOffset? NextInspectionDate,
    string? Remarks);

public record GetVehicleSafetyRecordsQuery : IRequest<IReadOnlyList<VehicleSafetyRecordDto>>;

public class GetVehicleSafetyRecordsQueryHandler(ITrafficSecurityDbContext context)
    : IRequestHandler<GetVehicleSafetyRecordsQuery, IReadOnlyList<VehicleSafetyRecordDto>>
{
    public async Task<IReadOnlyList<VehicleSafetyRecordDto>> Handle(
        GetVehicleSafetyRecordsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await context.VehicleSafetyRecords
            .OrderByDescending(x => x.InspectionDate)
            .Select(x => new VehicleSafetyRecordDto(
                x.Id,
                x.VehicleRegistrationNumber,
                x.VehicleId,
                x.Status,
                x.InspectionDate,
                x.NextInspectionDate,
                x.Remarks))
            .ToListAsync(cancellationToken);

        return result;
    }
}
