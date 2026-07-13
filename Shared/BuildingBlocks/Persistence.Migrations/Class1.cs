using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Usm.Shared.BuildingBlocks.Persistence.Migrations;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync<TDbContext>(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        where TDbContext : DbContext
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
