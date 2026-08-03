using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DocumentShare.Infrastructure.Persistence;

public sealed class DocumentShareDbContextFactory : IDesignTimeDbContextFactory<DocumentShareDbContext>
{
    public DocumentShareDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("DOCUMENTSHARE_DESIGNTIME_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_admin_dev";

        var optionsBuilder = new DbContextOptionsBuilder<DocumentShareDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x =>
        {
            x.MigrationsHistoryTable("__EFMigrationsHistory", "documentshare");
            x.EnableRetryOnFailure(3);
        });

        return new DocumentShareDbContext(optionsBuilder.Options);
    }
}