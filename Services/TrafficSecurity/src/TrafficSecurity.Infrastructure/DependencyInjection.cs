using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrafficSecurity.Application.Abstractions;
using Usm.Shared.BuildingBlocks.Localization;
using Usm.Shared.BuildingBlocks.Messaging;
using Usm.Shared.BuildingBlocks.Persistence.Migrations;
using Usm.Shared.Data.DbContextExtensions;
using Usm.Shared.Data.Scalability.Extensions;
using TrafficSecurity.Infrastructure.Persistence;

namespace TrafficSecurity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTrafficSecurityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string is required.");
        services.AddServiceDbContext<TrafficSecurityDbContext>(connectionString, "trafficsecurity");
        services.AddScoped<ITrafficSecurityDbContext>(sp => sp.GetRequiredService<TrafficSecurityDbContext>());
        services.AddRabbitMqMessaging(configuration);
        services.AddResxLocalization();
        services.AddAutoMigrations<TrafficSecurityDbContext>();

        services.AddDatabaseScaling(configuration);
        services.AddReadReplication(configuration);
        return services;
    }
}