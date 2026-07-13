using RepairMaintenance.Domain.RepairOrders;

namespace RepairMaintenance.Application.Abstractions;

public interface IRepairMaintenanceDbContext
{
    IQueryable<RepairOrder> RepairOrders { get; }
}
