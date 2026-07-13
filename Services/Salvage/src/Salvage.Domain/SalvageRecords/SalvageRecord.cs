using Salvage.Domain.Common;
using Usm.Shared.Data.DbContextExtensions;

namespace Salvage.Domain.SalvageRecords;

public enum SalvageStatus { Pending, Approved, Rejected, Disposed }

public sealed class SalvageRecord : AggregateRoot<Guid>, IAuditable
{
    private SalvageRecord() { }

    public string RecordNumber { get; private set; } = string.Empty;
    public Guid InventoryItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset SalvageDate { get; private set; }
    public string? ApprovedBy { get; private set; }
    public SalvageStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public static SalvageRecord Create(string recordNumber, Guid inventoryItemId, decimal quantity, string reason, DateTimeOffset salvageDate)
    {
        return new SalvageRecord
        {
            Id = Guid.NewGuid(),
            RecordNumber = recordNumber,
            InventoryItemId = inventoryItemId,
            Quantity = quantity,
            Reason = reason,
            SalvageDate = salvageDate,
            Status = SalvageStatus.Pending
        };
    }

    public void Approve(string approvedBy)
    {
        Status = SalvageStatus.Approved;
        ApprovedBy = approvedBy;
    }

    public void Reject()
    {
        Status = SalvageStatus.Rejected;
    }
}
