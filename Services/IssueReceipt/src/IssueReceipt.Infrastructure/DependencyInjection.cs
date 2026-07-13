using IssueReceipt.Application.Abstractions;
using IssueReceipt.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.Localization;
using Usm.Shared.BuildingBlocks.Messaging;
using Usm.Shared.BuildingBlocks.Persistence.Migrations;
using Usm.Shared.Data.DbContextExtensions;

namespace IssueReceipt.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIssueReceiptInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string is required");

        services.AddServiceDbContext<IssueReceiptDbContext>(connectionString, "issuereceipt");
        services.AddScoped<IIssueReceiptDbContext>(sp => sp.GetRequiredService<IssueReceiptDbContext>());
        services.AddRabbitMqMessaging(configuration);
        services.AddResxLocalization();
        services.AddAutoMigrations<IssueReceiptDbContext>();
        return services;
    }
}
