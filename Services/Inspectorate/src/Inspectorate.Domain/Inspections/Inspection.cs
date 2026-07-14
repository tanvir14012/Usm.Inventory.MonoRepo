using Inspectorate.Domain.Common;
using Usm.Shared.Data.DbContextExtensions;

namespace Inspectorate.Domain.Inspections;

public enum InspectionStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}

public class Inspection : AggregateRoot<Guid>, IAuditable
{
    public string InspectionNumber { get; private set; } = string.Empty;
    public Guid SubjectId { get; private set; }
    public string SubjectType { get; private set; } = string.Empty;
    public Guid InspectorId { get; private set; }
    public DateTimeOffset ScheduledDate { get; private set; }
    public DateTimeOffset? ConductedDate { get; private set; }
    public InspectionStatus Status { get; private set; }
    public string? Findings { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Inspection() { }

    public static Inspection Create(
        string inspectionNumber,
        Guid subjectId,
        string subjectType,
        Guid inspectorId,
        DateTimeOffset scheduledDate)
    {
        return new Inspection
        {
            Id = Guid.NewGuid(),
            InspectionNumber = inspectionNumber,
            SubjectId = subjectId,
            SubjectType = subjectType,
            InspectorId = inspectorId,
            ScheduledDate = scheduledDate,
            Status = InspectionStatus.Scheduled
        };
    }

    public void Complete(DateTimeOffset conductedDate, string? findings = null)
    {
        Status = InspectionStatus.Completed;
        ConductedDate = conductedDate;
        Findings = findings;
    }

    public void Cancel()
    {
        Status = InspectionStatus.Cancelled;
    }
}
