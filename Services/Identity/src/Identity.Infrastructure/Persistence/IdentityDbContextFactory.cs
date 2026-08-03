using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Persistence;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("IDENTITY_DESIGNTIME_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_admin_dev";

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x =>
        {
            x.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
            x.EnableRetryOnFailure(3);
        });

        return new IdentityDbContext(optionsBuilder.Options);
    }
}