using MediatR;

namespace Procurement.Application.PurchaseOrders.Queries;

public sealed record GetPurchaseOrdersQuery : IRequest<IReadOnlyList<PurchaseOrderDto>>;
