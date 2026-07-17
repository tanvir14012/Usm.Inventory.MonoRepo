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
    public string VehicleRegistrationNumber { get; private set; } = string.Empty;
    public Guid VehicleId { get; private set; }
    public DateTimeOffset InspectionDate { get; private set; }
    public Guid InspectorId { get; private set; }
    public SafetyStatus Status { get; private set; }
    public string? Remarks { get; private set; }
    public DateTimeOffset? NextInspectionDate { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    private VehicleSafetyRecord() { }

    public static VehicleSafetyRecord Create(string vehicleRegistrationNumber, Guid vehicleId, DateTimeOffset inspectionDate, Guid inspectorId)
    {
        return new VehicleSafetyRecord
        {
            Id = Guid.NewGuid(),
            VehicleRegistrationNumber = vehicleRegistrationNumber,
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

    public void RequiresAttention(string remarks, DateTimeOffset nextInspectionDate)
    {
        Status = SafetyStatus.RequiresAttention;
        Remarks = remarks;
        NextInspectionDate = nextInspectionDate;
    }
}
