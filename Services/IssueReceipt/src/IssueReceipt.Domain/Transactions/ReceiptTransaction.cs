using IssueReceipt.Domain.Common;
using Usm.Shared.Data.DbContextExtensions;

namespace IssueReceipt.Domain.Transactions;

public sealed class ReceiptTransaction : AggregateRoot<Guid>, IAuditable
{
    private ReceiptTransaction() { }

    public string TransactionNumber { get; private set; } = string.Empty;
    public Guid InventoryItemId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public decimal Quantity { get; private set; }
    public string ReceivedFrom { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public static ReceiptTransaction Create(
        string transactionNumber, Guid inventoryItemId, Guid warehouseId,
        decimal quantity, string receivedFrom, DateTimeOffset receivedDate, string? notes = null)
    {
        return new ReceiptTransaction
        {
            Id = Guid.NewGuid(),
            TransactionNumber = transactionNumber,
            InventoryItemId = inventoryItemId,
            WarehouseId = warehouseId,
            Quantity = quantity,
            ReceivedFrom = receivedFrom,
            ReceivedDate = receivedDate,
            Notes = notes
        };
    }
}
