using BudgetPlanning.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.Localization;
using Usm.Shared.BuildingBlocks.Messaging;
using Usm.Shared.BuildingBlocks.Persistence.Migrations;
using Usm.Shared.Data.DbContextExtensions;

namespace BudgetPlanning.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBudgetPlanningInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string is required.");
        services.AddServiceDbContext<BudgetPlanningDbContext>(cs, "budgetplanning");
        services.AddRabbitMqMessaging(configuration);
        services.AddResxLocalization();
        services.AddAutoMigrations<BudgetPlanningDbContext>();
        return services;
    }
}
