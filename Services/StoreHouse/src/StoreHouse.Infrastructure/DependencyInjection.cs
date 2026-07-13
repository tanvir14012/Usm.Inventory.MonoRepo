using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StoreHouse.Application.Abstractions;
using StoreHouse.Infrastructure.Persistence;
using Usm.Shared.BuildingBlocks.Localization;
using Usm.Shared.BuildingBlocks.Messaging;
using Usm.Shared.BuildingBlocks.Persistence.Migrations;
using Usm.Shared.Data.DbContextExtensions;

namespace StoreHouse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddStoreHouseInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string is required");

        services.AddServiceDbContext<StoreHouseDbContext>(connectionString, "storehouse");
        services.AddScoped<IStoreHouseDbContext>(sp => sp.GetRequiredService<StoreHouseDbContext>());
        services.AddRabbitMqMessaging(configuration);
        services.AddResxLocalization();
        services.AddAutoMigrations<StoreHouseDbContext>();
        return services;
    }
}
