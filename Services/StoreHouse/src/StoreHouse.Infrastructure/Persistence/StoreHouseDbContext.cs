using Microsoft.EntityFrameworkCore;
using StoreHouse.Application.Abstractions;
using StoreHouse.Domain.InventoryItems;
using StoreHouse.Domain.Warehouses;
using Usm.Shared.Data.DbContextExtensions;

namespace StoreHouse.Infrastructure.Persistence;

public class StoreHouseDbContext(DbContextOptions<StoreHouseDbContext> options)
    : ServiceDbContext(options, "storehouse"), IStoreHouseDbContext
{
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
}
