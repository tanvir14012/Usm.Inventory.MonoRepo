using RepairMaintenance.Domain.Common;
using Usm.Shared.Data.DbContextExtensions;

namespace RepairMaintenance.Domain.RepairOrders;

public enum RepairOrderStatus { Pending, InProgress, Completed, Cancelled }

public sealed class RepairOrder : AggregateRoot<Guid>, IAuditable
{
    private RepairOrder() { }

    public string OrderNumber { get; private set; } = string.Empty;
    public Guid InventoryItemId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public RepairOrderStatus Status { get; private set; }
    public Guid? AssignedTechnicianId { get; private set; }
    public DateTimeOffset ReportedDate { get; private set; }
    public DateTimeOffset? CompletedDate { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public static RepairOrder Create(string orderNumber, Guid inventoryItemId, string description, DateTimeOffset reportedDate)
    {
        return new RepairOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            InventoryItemId = inventoryItemId,
            Description = description,
            Status = RepairOrderStatus.Pending,
            ReportedDate = reportedDate
        };
    }

    public void Complete(DateTimeOffset completedDate)
    {
        Status = RepairOrderStatus.Completed;
        CompletedDate = completedDate;
    }

    public void Assign(Guid technicianId)
    {
        AssignedTechnicianId = technicianId;
    }
}
