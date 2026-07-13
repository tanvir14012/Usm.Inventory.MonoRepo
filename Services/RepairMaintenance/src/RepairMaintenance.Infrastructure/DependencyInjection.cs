using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepairMaintenance.Application.Abstractions;
using RepairMaintenance.Infrastructure.Persistence;
using Usm.Shared.BuildingBlocks.Localization;
using Usm.Shared.BuildingBlocks.Messaging;
using Usm.Shared.BuildingBlocks.Persistence.Migrations;
using Usm.Shared.Data.DbContextExtensions;

namespace RepairMaintenance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRepairMaintenanceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string is required");

        services.AddServiceDbContext<RepairMaintenanceDbContext>(connectionString, "repairmaintenance");
        services.AddScoped<IRepairMaintenanceDbContext>(sp => sp.GetRequiredService<RepairMaintenanceDbContext>());
        services.AddRabbitMqMessaging(configuration);
        services.AddResxLocalization();
        services.AddAutoMigrations<RepairMaintenanceDbContext>();
        return services;
    }
}
