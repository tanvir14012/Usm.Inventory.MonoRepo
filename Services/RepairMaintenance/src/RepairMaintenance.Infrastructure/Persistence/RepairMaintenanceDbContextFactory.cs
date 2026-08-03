using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RepairMaintenance.Infrastructure.Persistence;

public sealed class RepairMaintenanceDbContextFactory : IDesignTimeDbContextFactory<RepairMaintenanceDbContext>
{
    public RepairMaintenanceDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("REPAIRMAINTENANCE_DESIGNTIME_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_admin_dev";

        var optionsBuilder = new DbContextOptionsBuilder<RepairMaintenanceDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x =>
        {
            x.MigrationsHistoryTable("__EFMigrationsHistory", "repairmaintenance");
            x.EnableRetryOnFailure(3);
        });

        return new RepairMaintenanceDbContext(optionsBuilder.Options);
    }
}