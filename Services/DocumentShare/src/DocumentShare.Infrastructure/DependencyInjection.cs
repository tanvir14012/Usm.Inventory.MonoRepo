using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.Localization;
using Usm.Shared.BuildingBlocks.Messaging;
using Usm.Shared.BuildingBlocks.Persistence.Migrations;
using Usm.Shared.Data.DbContextExtensions;
using Usm.Shared.Data.Scalability.Extensions;
using DocumentShare.Infrastructure.Persistence;
using Usm.Shared.Infrastructure.CDN.Extensions;

namespace DocumentShare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDocumentShareInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string is required.");
        services.AddServiceDbContext<DocumentShareDbContext>(connectionString, "documentshare");
        services.AddRabbitMqMessaging(configuration);
        services.AddResxLocalization();
        services.AddAutoMigrations<DocumentShareDbContext>();

        // DB-level scaling: read-replica splitting + global scaling options
        services.AddDatabaseScaling(configuration);
        services.AddReadReplication(configuration);

        // CDN infrastructure for high-performance document delivery
        services.AddCdnInfrastructure(configuration);
        return services;
    }
}
