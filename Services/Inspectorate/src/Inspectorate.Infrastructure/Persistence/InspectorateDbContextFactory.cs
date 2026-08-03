using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inspectorate.Infrastructure.Persistence;

public sealed class InspectorateDbContextFactory : IDesignTimeDbContextFactory<InspectorateDbContext>
{
    public InspectorateDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("INSPECTORATE_DESIGNTIME_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_admin_dev";

        var optionsBuilder = new DbContextOptionsBuilder<InspectorateDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x =>
        {
            x.MigrationsHistoryTable("__EFMigrationsHistory", "inspectorate");
            x.EnableRetryOnFailure(3);
        });

        return new InspectorateDbContext(optionsBuilder.Options);
    }
}