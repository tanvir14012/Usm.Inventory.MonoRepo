using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StoreHouse.Infrastructure.Persistence;

public sealed class StoreHouseDbContextFactory : IDesignTimeDbContextFactory<StoreHouseDbContext>
{
    public StoreHouseDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("STOREHOUSE_DESIGNTIME_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_admin_dev";

        var optionsBuilder = new DbContextOptionsBuilder<StoreHouseDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x =>
        {
            x.MigrationsHistoryTable("__EFMigrationsHistory", "storehouse");
            x.EnableRetryOnFailure(3);
        });

        return new StoreHouseDbContext(optionsBuilder.Options);
    }
}
