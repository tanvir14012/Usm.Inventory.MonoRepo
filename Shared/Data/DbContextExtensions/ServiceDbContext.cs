using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Usm.Shared.Data.DbContextExtensions;

/// <summary>
/// Base DbContext that applies schema isolation and common conventions.
/// Each service DbContext inherits from this and passes its own schema name.
/// </summary>
public abstract class ServiceDbContext(DbContextOptions options, string schema) : DbContext(options)
{
    protected readonly string Schema = schema;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void StampAuditFields()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                    auditable.CreatedAt = DateTimeOffset.UtcNow;
                else if (entry.State == EntityState.Modified)
                    auditable.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }
}

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
}

public static class DbContextServiceExtensions
{
    public static IServiceCollection AddServiceDbContext<TContext>(
        this IServiceCollection services,
        string connectionString,
        string schema)
        where TContext : ServiceDbContext
    {
        services.AddDbContext<TContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema);
                npgsql.EnableRetryOnFailure(3);
            }));

        return services;
    }
}
