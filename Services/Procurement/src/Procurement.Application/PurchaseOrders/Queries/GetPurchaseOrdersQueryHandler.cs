using MediatR;

namespace Procurement.Application.PurchaseOrders.Queries;

public sealed class GetPurchaseOrdersQueryHandler : IRequestHandler<GetPurchaseOrdersQuery, IReadOnlyList<PurchaseOrderDto>>
{
    public Task<IReadOnlyList<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PurchaseOrderDto>>(Array.Empty<PurchaseOrderDto>());
    }
}
