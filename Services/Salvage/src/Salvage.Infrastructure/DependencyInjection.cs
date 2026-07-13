using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Salvage.Application.Abstractions;
using Salvage.Infrastructure.Persistence;
using Usm.Shared.BuildingBlocks.Localization;
using Usm.Shared.BuildingBlocks.Messaging;
using Usm.Shared.BuildingBlocks.Persistence.Migrations;
using Usm.Shared.Data.DbContextExtensions;

namespace Salvage.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSalvageInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string is required");

        services.AddServiceDbContext<SalvageDbContext>(connectionString, "salvage");
        services.AddScoped<ISalvageDbContext>(sp => sp.GetRequiredService<SalvageDbContext>());
        services.AddRabbitMqMessaging(configuration);
        services.AddResxLocalization();
        services.AddAutoMigrations<SalvageDbContext>();
        return services;
    }
}
