using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TrafficSecurity.Infrastructure.Persistence;

public sealed class TrafficSecurityDbContextFactory : IDesignTimeDbContextFactory<TrafficSecurityDbContext>
{
    public TrafficSecurityDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("TRAFFICSECURITY_DESIGNTIME_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_admin_dev";

        var optionsBuilder = new DbContextOptionsBuilder<TrafficSecurityDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x =>
        {
            x.MigrationsHistoryTable("__EFMigrationsHistory", "trafficsecurity");
            x.EnableRetryOnFailure(3);
        });

        return new TrafficSecurityDbContext(optionsBuilder.Options);
    }
}
