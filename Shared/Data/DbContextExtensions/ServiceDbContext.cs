using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Usm.Shared.Contracts.Localization;
using Usm.Shared.EntityFramework.Caching.Extensions;

namespace Usm.Shared.Data.DbContextExtensions;

/// <summary>
/// Base DbContext that applies schema isolation and common conventions.
/// Each service DbContext inherits from this and passes its own schema name.
/// </summary>
public abstract class ServiceDbContext(DbContextOptions options, string schema) : DbContext(options)
{
    protected readonly string Schema = schema;
    private static readonly JsonSerializerOptions LocalizedTextJsonSerializerOptions = new(JsonSerializerDefaults.Web);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        ApplyLocalizedJsonbConventions(modelBuilder);
    }

    private static void ApplyLocalizedJsonbConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var localizedProperties = entityType.GetProperties()
                .Where(property => property.ClrType == typeof(LocalizedText) && property.GetValueConverter() is null)
                .ToArray();

            if (localizedProperties.Length == 0)
                continue;

            var entityBuilder = modelBuilder.Entity(entityType.ClrType);
            foreach (var property in localizedProperties)
            {
                var localizedProperty = entityBuilder.Property<LocalizedText>(property.Name);
                localizedProperty.HasConversion(
                    value => JsonSerializer.Serialize(value, LocalizedTextJsonSerializerOptions),
                    value => JsonSerializer.Deserialize<LocalizedText>(value, LocalizedTextJsonSerializerOptions) ?? LocalizedText.Empty);
                localizedProperty.HasColumnType("jsonb");
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void StampAuditFields()
    {
        var actorId = AuditActorContext.ActorId;
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                    if (actorId.HasValue)
                        auditable.CreatedBy = actorId.Value;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedAt = now;
                    if (actorId.HasValue)
                        auditable.UpdatedBy = actorId.Value;
                }
            }
        }
    }
}

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }

    Guid? CreatedBy { get; set; }
    Guid? UpdatedBy { get; set; }
}

public static class AuditActorContext
{
    private static readonly AsyncLocal<Guid?> CurrentActorId = new();

    public static Guid? ActorId => CurrentActorId.Value;

    public static IDisposable Use(Guid? actorId)
    {
        var previous = CurrentActorId.Value;
        CurrentActorId.Value = actorId;
        return new RestoreScope(() => CurrentActorId.Value = previous);
    }

    private sealed class RestoreScope(Action restore) : IDisposable
    {
        private readonly Action _restore = restore;
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _restore();
            _disposed = true;
        }
    }
}

public static class DbContextServiceExtensions
{
    public static IServiceCollection AddServiceDbContext<TContext>(
        this IServiceCollection services,
        string connectionString,
        string schema)
        where TContext : ServiceDbContext
    {
        services.AddEntityFrameworkCaching();

        services.AddDbContext<TContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema);
                npgsql.EnableRetryOnFailure(3);
            });

            var interceptors = serviceProvider.GetServices<IInterceptor>().ToArray();
            if (interceptors.Length > 0)
                options.AddInterceptors(interceptors);
        });

        return services;
    }
}
