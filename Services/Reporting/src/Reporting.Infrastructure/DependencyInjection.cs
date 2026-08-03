using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reporting.Infrastructure.Persistence;
using Usm.Shared.BuildingBlocks.Localization;
using Usm.Shared.BuildingBlocks.Messaging;
using Usm.Shared.BuildingBlocks.Persistence.Migrations;
using Usm.Shared.Data.DbContextExtensions;
using Usm.Shared.Data.Scalability.Extensions;

namespace Reporting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string is required.");
        services.AddServiceDbContext<ReportingDbContext>(cs, "reporting");
        services.AddRabbitMqMessaging(configuration);
        services.AddResxLocalization();
        services.AddAutoMigrations<ReportingDbContext>();

        services.AddDatabaseScaling(configuration);
        services.AddReadReplication(configuration);
        return services;
    }
}