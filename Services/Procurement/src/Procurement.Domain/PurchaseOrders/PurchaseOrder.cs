using Procurement.Domain.Common;
using Usm.Shared.Data.DbContextExtensions;

namespace Procurement.Domain.PurchaseOrders;

public enum PurchaseOrderStatus { Draft, Submitted, Approved, Delivered, Cancelled }

public sealed class PurchaseOrder : AggregateRoot<Guid>, IAuditable
{
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid SupplierId { get; private set; }
    public DateTimeOffset OrderDate { get; private set; }
    public DateTimeOffset? ExpectedDeliveryDate { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private PurchaseOrder() { }

    public static PurchaseOrder Create(string orderNumber, Guid supplierId, DateTimeOffset orderDate, DateTimeOffset? expectedDeliveryDate = null)
    {
        return new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            SupplierId = supplierId,
            OrderDate = orderDate,
            ExpectedDeliveryDate = expectedDeliveryDate,
            Status = PurchaseOrderStatus.Draft,
            TotalAmount = 0m,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Submit() => Status = PurchaseOrderStatus.Submitted;
    public void Approve() => Status = PurchaseOrderStatus.Approved;
    public void MarkDelivered() => Status = PurchaseOrderStatus.Delivered;
}
