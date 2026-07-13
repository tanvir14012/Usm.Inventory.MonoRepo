using Microsoft.EntityFrameworkCore;
using Procurement.Domain.PurchaseOrders;
using Procurement.Domain.Suppliers;
using Usm.Shared.Data.DbContextExtensions;

namespace Procurement.Infrastructure.Persistence;

public sealed class ProcurementDbContext(DbContextOptions<ProcurementDbContext> options)
    : ServiceDbContext(options, "procurement")
{
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
}
