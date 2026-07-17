using Microsoft.EntityFrameworkCore;
using Procurement.Domain.PurchaseOrders;
using Procurement.Domain.Suppliers;

namespace Procurement.Application.Abstractions;

public interface IProcurementDbContext
{
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<Supplier> Suppliers { get; }
}
