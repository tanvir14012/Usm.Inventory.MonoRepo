using MediatR;
using StoreHouse.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace StoreHouse.Application.InventoryItems.Queries;

public record GetInventoryItemsQuery : IRequest<IReadOnlyList<InventoryItemDto>>;

public record InventoryItemDto(
    Guid Id,
    string NameEn,
    string Code,
    string Unit,
    decimal CurrentQuantity,
    decimal ReorderLevel,
    bool IsBelowReorderLevel);

public sealed class GetInventoryItemsQueryHandler(IStoreHouseDbContext context)
    : IRequestHandler<GetInventoryItemsQuery, IReadOnlyList<InventoryItemDto>>
{
    public async Task<IReadOnlyList<InventoryItemDto>> Handle(GetInventoryItemsQuery request, CancellationToken cancellationToken)
    {
        var result = await context.InventoryItems
            .OrderBy(x => x.Name.En)
            .Select(x => new InventoryItemDto(
                x.Id,
                x.Name.En,
                x.Code,
                x.Unit,
                x.CurrentQuantity,
                x.ReorderLevel,
                x.CurrentQuantity <= x.ReorderLevel))
            .ToListAsync(cancellationToken);

        return result;
    }
}
