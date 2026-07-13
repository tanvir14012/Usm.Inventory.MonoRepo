using IssueReceipt.Domain.Common;
using Usm.Shared.Data.DbContextExtensions;

namespace IssueReceipt.Domain.Transactions;

public sealed class IssueTransaction : AggregateRoot<Guid>, IAuditable
{
    private IssueTransaction() { }

    public string TransactionNumber { get; private set; } = string.Empty;
    public Guid InventoryItemId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public decimal Quantity { get; private set; }
    public string IssuedTo { get; private set; } = string.Empty;
    public DateTimeOffset IssuedDate { get; private set; }
    public string? Purpose { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public static IssueTransaction Create(
        string transactionNumber, Guid inventoryItemId, Guid warehouseId,
        decimal quantity, string issuedTo, DateTimeOffset issuedDate, string? purpose = null)
    {
        return new IssueTransaction
        {
            Id = Guid.NewGuid(),
            TransactionNumber = transactionNumber,
            InventoryItemId = inventoryItemId,
            WarehouseId = warehouseId,
            Quantity = quantity,
            IssuedTo = issuedTo,
            IssuedDate = issuedDate,
            Purpose = purpose
        };
    }
}
