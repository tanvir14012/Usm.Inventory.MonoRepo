using Procurement.Domain.PurchaseOrders;

namespace Procurement.Application.PurchaseOrders.Queries;

public sealed record PurchaseOrderDto(
    Guid Id,
    string OrderNumber,
    Guid SupplierId,
    PurchaseOrderStatus Status,
    decimal TotalAmount);
