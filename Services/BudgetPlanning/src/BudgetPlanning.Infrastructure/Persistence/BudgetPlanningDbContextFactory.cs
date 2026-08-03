using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BudgetPlanning.Infrastructure.Persistence;

public sealed class BudgetPlanningDbContextFactory : IDesignTimeDbContextFactory<BudgetPlanningDbContext>
{
    public BudgetPlanningDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("BUDGETPLANNING_DESIGNTIME_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_admin_dev";

        var optionsBuilder = new DbContextOptionsBuilder<BudgetPlanningDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x =>
        {
            x.MigrationsHistoryTable("__EFMigrationsHistory", "budgetplanning");
            x.EnableRetryOnFailure(3);
        });

        return new BudgetPlanningDbContext(optionsBuilder.Options);
    }
}