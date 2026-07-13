using TrafficSecurity.Domain.Common;
using Usm.Shared.Data.DbContextExtensions;

namespace TrafficSecurity.Domain.VehicleSafetyRecords;

public enum SafetyStatus
{
    Pending,
    Passed,
    Failed,
    RequiresAttention
}

public class VehicleSafetyRecord : AggregateRoot<Guid>, IAuditable
{
    public Guid VehicleId { get; private set; }
    public DateTimeOffset InspectionDate { get; private set; }
    public Guid InspectorId { get; private set; }
    public SafetyStatus Status { get; private set; }
    public string? Remarks { get; private set; }
    public DateTimeOffset? NextInspectionDate { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private VehicleSafetyRecord() { }

    public static VehicleSafetyRecord Create(Guid vehicleId, DateTimeOffset inspectionDate, Guid inspectorId)
    {
        return new VehicleSafetyRecord
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            InspectionDate = inspectionDate,
            InspectorId = inspectorId,
            Status = SafetyStatus.Pending
        };
    }

    public void Pass(DateTimeOffset nextInspectionDate, string? remarks = null)
    {
        Status = SafetyStatus.Passed;
        NextInspectionDate = nextInspectionDate;
        Remarks = remarks;
    }

    public void Fail(string remarks)
    {
        Status = SafetyStatus.Failed;
        Remarks = remarks;
    }
}
