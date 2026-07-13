using Identity.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.Localization;
using Usm.Shared.BuildingBlocks.Messaging;
using Usm.Shared.BuildingBlocks.Persistence.Migrations;
using Usm.Shared.Data.DbContextExtensions;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_pass";

        services.AddServiceDbContext<IdentityDbContext>(connectionString, "identity");

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<IdentityDbContext>();
            });

        services.AddRabbitMqMessaging(configuration);
        services.AddResxLocalization();
        services.AddAutoMigrations<IdentityDbContext>();

        return services;
    }
}
