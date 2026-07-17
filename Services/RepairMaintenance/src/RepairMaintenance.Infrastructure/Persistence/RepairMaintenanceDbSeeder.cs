using Microsoft.EntityFrameworkCore;
using RepairMaintenance.Domain.RepairOrders;

namespace RepairMaintenance.Infrastructure.Persistence;

public static class RepairMaintenanceDbSeeder
{
    public static async Task SeedAsync(RepairMaintenanceDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.RepairOrders.AnyAsync(cancellationToken))
        {
            return;
        }

        var order1 = RepairOrder.Create("RM-2401", Guid.NewGuid(), "Generator set failed load test", DateTimeOffset.UtcNow.AddDays(-8));
        order1.StartWork(Guid.NewGuid());

        var order2 = RepairOrder.Create("RM-2402", Guid.NewGuid(), "Night vision optics realignment", DateTimeOffset.UtcNow.AddDays(-4));
        order2.StartWork(Guid.NewGuid());
        order2.Complete(DateTimeOffset.UtcNow.AddDays(-1));

        var order3 = RepairOrder.Create("RM-2403", Guid.NewGuid(), "Convoy trailer brake replacement", DateTimeOffset.UtcNow.AddDays(-2));

        dbContext.RepairOrders.AddRange(order1, order2, order3);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
