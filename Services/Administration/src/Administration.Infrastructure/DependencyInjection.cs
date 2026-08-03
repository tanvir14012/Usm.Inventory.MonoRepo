using Administration.Application.Abstractions;
using Administration.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.Localization;
using Usm.Shared.BuildingBlocks.Messaging;
using Usm.Shared.BuildingBlocks.Persistence.Migrations;
using Usm.Shared.Data.DbContextExtensions;
using Usm.Shared.Data.Scalability.Extensions;

namespace Administration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAdministrationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;******";

        services.AddServiceDbContext<AdministrationDbContext>(connectionString, "administration");
        services.AddScoped<IAdministrationDbContext>(sp => sp.GetRequiredService<AdministrationDbContext>());
        services.AddRabbitMqMessaging(configuration);
        services.AddResxLocalization();
        services.AddAutoMigrations<AdministrationDbContext>();

        // DB-level scaling: read-replica splitting + global scaling options
        services.AddDatabaseScaling(configuration);
        services.AddReadReplication(configuration);

        return services;
    }
}
