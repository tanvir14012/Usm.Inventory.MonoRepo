using MediatR;
using StoreHouse.Application.Abstractions;

namespace StoreHouse.Application.InventoryItems.Queries;

public record GetInventoryItemsQuery : IRequest<IReadOnlyList<InventoryItemDto>>;

public record InventoryItemDto(Guid Id, string Code, decimal CurrentQuantity, decimal ReorderLevel);

public sealed class GetInventoryItemsQueryHandler(IStoreHouseDbContext context)
    : IRequestHandler<GetInventoryItemsQuery, IReadOnlyList<InventoryItemDto>>
{
    public Task<IReadOnlyList<InventoryItemDto>> Handle(GetInventoryItemsQuery request, CancellationToken cancellationToken)
    {
        var result = context.InventoryItems
            .Select(x => new InventoryItemDto(x.Id, x.Code, x.CurrentQuantity, x.ReorderLevel))
            .ToList();
        return Task.FromResult<IReadOnlyList<InventoryItemDto>>(result);
    }
}
