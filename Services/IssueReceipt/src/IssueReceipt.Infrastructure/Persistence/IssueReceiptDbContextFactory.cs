using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IssueReceipt.Infrastructure.Persistence;

public sealed class IssueReceiptDbContextFactory : IDesignTimeDbContextFactory<IssueReceiptDbContext>
{
    public IssueReceiptDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ISSUERECEIPT_DESIGNTIME_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_admin_dev";

        var optionsBuilder = new DbContextOptionsBuilder<IssueReceiptDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x =>
        {
            x.MigrationsHistoryTable("__EFMigrationsHistory", "issuereceipt");
            x.EnableRetryOnFailure(3);
        });

        return new IssueReceiptDbContext(optionsBuilder.Options);
    }
}