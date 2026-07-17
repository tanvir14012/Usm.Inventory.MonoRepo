using MediatR;
using Microsoft.EntityFrameworkCore;
using Procurement.Application.Abstractions;
using Usm.Shared.EntityFramework.Caching.Extensions;

namespace Procurement.Application.PurchaseOrders.Queries;

public sealed class GetPurchaseOrdersQueryHandler(IProcurementDbContext context)
    : IRequestHandler<GetPurchaseOrdersQuery, IReadOnlyList<PurchaseOrderDto>>
{
    public async Task<IReadOnlyList<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        var suppliers = await context.Suppliers
            .AsNoTracking()
            .OrderBy(x => x.Name.En)
            .CacheAsync("procurement-suppliers", cancellationToken: cancellationToken);

        var supplierLookup = suppliers.ToDictionary(x => x.Id, x => x.Name.En);

        var orders = await context.PurchaseOrders
            .AsNoTracking()
            .OrderByDescending(x => x.OrderDate)
            .CacheAsync("procurement-orders", cancellationToken: cancellationToken);

        return orders
            .Select(x => new PurchaseOrderDto(
                x.Id,
                x.OrderNumber,
                supplierLookup.GetValueOrDefault(x.SupplierId, "Unknown Supplier"),
                x.SupplierId,
                x.Status,
                x.TotalAmount,
                x.OrderDate,
                x.ExpectedDeliveryDate))
            .ToArray();
    }
}
