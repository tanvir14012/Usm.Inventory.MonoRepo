using Microsoft.EntityFrameworkCore;
using RepairMaintenance.Application.Abstractions;
using RepairMaintenance.Domain.RepairOrders;
using Usm.Shared.Data.DbContextExtensions;

namespace RepairMaintenance.Infrastructure.Persistence;

public class RepairMaintenanceDbContext(DbContextOptions<RepairMaintenanceDbContext> options)
    : ServiceDbContext(options, "repairmaintenance"), IRepairMaintenanceDbContext
{
    public DbSet<RepairOrder> RepairOrders => Set<RepairOrder>();
}
