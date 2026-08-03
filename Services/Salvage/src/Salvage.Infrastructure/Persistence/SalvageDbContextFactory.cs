using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Salvage.Infrastructure.Persistence;

public sealed class SalvageDbContextFactory : IDesignTimeDbContextFactory<SalvageDbContext>
{
    public SalvageDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("SALVAGE_DESIGNTIME_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_admin_dev";

        var optionsBuilder = new DbContextOptionsBuilder<SalvageDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x =>
        {
            x.MigrationsHistoryTable("__EFMigrationsHistory", "salvage");
            x.EnableRetryOnFailure(3);
        });

        return new SalvageDbContext(optionsBuilder.Options);
    }
}