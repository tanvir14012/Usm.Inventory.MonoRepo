using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.Localization;
using Usm.Shared.BuildingBlocks.Messaging;
using Usm.Shared.BuildingBlocks.Persistence.Migrations;
using Usm.Shared.Data.DbContextExtensions;
using Inspectorate.Infrastructure.Persistence;

namespace Inspectorate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInspectorateInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string is required.");
        services.AddServiceDbContext<InspectorateDbContext>(connectionString, "inspectorate");
        services.AddRabbitMqMessaging(configuration);
        services.AddResxLocalization();
        services.AddAutoMigrations<InspectorateDbContext>();
        return services;
    }
}
