using StoreHouse.Domain.InventoryItems;
using StoreHouse.Domain.Warehouses;

namespace StoreHouse.Application.Abstractions;

public interface IStoreHouseDbContext
{
    IQueryable<InventoryItem> InventoryItems { get; }
    IQueryable<Warehouse> Warehouses { get; }
}
