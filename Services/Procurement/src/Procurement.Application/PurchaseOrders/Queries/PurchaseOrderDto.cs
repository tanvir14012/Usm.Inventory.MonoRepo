using Procurement.Domain.PurchaseOrders;

namespace Procurement.Application.PurchaseOrders.Queries;

public sealed record PurchaseOrderDto(
    Guid Id,
    string OrderNumber,
    string SupplierName,
    Guid SupplierId,
    PurchaseOrderStatus Status,
    decimal TotalAmount,
    DateTimeOffset OrderDate,
    DateTimeOffset? ExpectedDeliveryDate);
