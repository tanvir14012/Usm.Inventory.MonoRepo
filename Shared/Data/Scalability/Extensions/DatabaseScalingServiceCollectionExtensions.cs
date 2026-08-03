using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Data.Scalability.Abstractions;
using Usm.Shared.Data.Scalability.Options;
using Usm.Shared.Data.Scalability.Partitioning;
using Usm.Shared.Data.Scalability.Polling;
using Usm.Shared.Data.Scalability.Replication;
using Usm.Shared.Data.Scalability.ScriptSeeding;
using Usm.Shared.Data.Scalability.Sharding;
using Usm.Shared.Data.Scalability.Strategies;

namespace Usm.Shared.Data.Scalability.Extensions;

/// <summary>
/// Fluent DI registration extensions for the database scalability infrastructure.
/// <para>
/// Typical service registration:
/// <code>
/// services
///     .AddDatabaseScaling(configuration)           // core options
///     .AddReadReplication(configuration)            // read/write splitting
///     .AddSharding&lt;Product&gt;(config, p => p.CategoryId.ToString())
///     .AddScriptMigrations(configuration)           // SQL seed files
///     .AddEfCoreOutboxPoller&lt;AppDbContext, OutboxMessage&gt;(configuration);
/// </code>
/// </para>
/// </summary>
public static class DatabaseScalingServiceCollectionExtensions
{
    // ── Core ──────────────────────────────────────────────────────────────────

    /// <summary>Registers global scaling options from configuration.</summary>
    public static IServiceCollection AddDatabaseScaling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseScalingOptions>(
            configuration.GetSection(DatabaseScalingOptions.SectionName));
        return services;
    }

    // ── Strategy orchestrator ─────────────────────────────────────────────────

    /// <summary>
    /// Registers <see cref="DatabaseScalingOrchestrator{TEntity}"/> as a scoped service
    /// for the specified entity type. All <see cref="IDatabaseScalingStrategy{TEntity}"/>
    /// implementations already in the container are automatically composed.
    /// </summary>
    public static IServiceCollection AddScalingOrchestratorFor<TEntity>(
        this IServiceCollection services)
        where TEntity : class
    {
        services.TryAddScoped<DatabaseScalingOrchestrator<TEntity>>();
        return services;
    }

    // ── Read/Write Replication ────────────────────────────────────────────────

    /// <summary>
    /// Adds the <see cref="ReadReplicaCommandInterceptor"/> and registers it as a scoped
    /// EF Core interceptor. Activate per-request with <see cref="ReadReplicaContext.UseReadReplica"/>.
    /// </summary>
    public static IServiceCollection AddReadReplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ReplicationOptions>(
            configuration.GetSection(ReplicationOptions.SectionName));
        RegisterReplicationInterceptor(services);
        return services;
    }

    /// <summary>Adds replication with inline options configuration.</summary>
    public static IServiceCollection AddReadReplication(
        this IServiceCollection services,
        Action<ReplicationOptions> configure)
    {
        services.Configure(configure);
        RegisterReplicationInterceptor(services);
        return services;
    }

    private static void RegisterReplicationInterceptor(IServiceCollection services)
    {
        services.TryAddScoped<ReadReplicaCommandInterceptor>();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IInterceptor, ReadReplicaCommandInterceptor>());
    }

    // ── Sharding ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers the <see cref="DefaultShardRouter{TEntity}"/> for the specified entity type
    /// with a shard-key selector lambda.
    /// </summary>
    public static IServiceCollection AddSharding<TEntity>(
        this IServiceCollection services,
        IConfiguration configuration,
        Func<TEntity, string> shardKeySelector)
        where TEntity : class
    {
        services.Configure<ShardingOptions>(
            configuration.GetSection(ShardingOptions.SectionName));

        services.TryAddSingleton<IShardRouter<TEntity>>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ShardingOptions>>();
            return new DefaultShardRouter<TEntity>(opts, shardKeySelector);
        });

        return services;
    }

    // ── Partition diagnostics ─────────────────────────────────────────────────

    /// <summary>
    /// Adds the <see cref="PartitionQueryInterceptor"/> (diagnostic only).
    /// Logs warnings when queries omit the partition-key predicate.
    /// Intended for development / staging environments.
    /// </summary>
    public static IServiceCollection AddPartitionDiagnostics(
        this IServiceCollection services,
        Action<PartitionQueryInterceptor>? configure = null)
    {
        services.TryAddSingleton<PartitionQueryInterceptor>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IInterceptor, PartitionQueryInterceptor>());

        if (configure is not null)
        {
            using var sp = services.BuildServiceProvider();
            var interceptor = sp.GetRequiredService<PartitionQueryInterceptor>();
            configure(interceptor);
        }

        return services;
    }

    // ── Script Seeding ────────────────────────────────────────────────────────

    /// <summary>
    /// Registers the <see cref="ScriptMigrationEngine"/> and a startup
    /// <see cref="ScriptMigrationHostedService"/> that runs seed scripts before the
    /// application accepts traffic.
    /// </summary>
    public static IServiceCollection AddScriptMigrations(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ScriptSeedingOptions>? configure = null)
    {
        services.Configure<ScriptSeedingOptions>(
            configuration.GetSection(ScriptSeedingOptions.SectionName));

        if (configure is not null)
            services.PostConfigure(configure);

        services.TryAddSingleton<IScriptMigrationEngine, ScriptMigrationEngine>();
        services.AddHostedService<ScriptMigrationHostedService>();
        return services;
    }

    // ── Outbox Polling ────────────────────────────────────────────────────────

    /// <summary>
    /// Registers a custom <typeparamref name="TPoller"/> as <see cref="IOutboxPoller{TMessage}"/>.
    /// The background service must be registered separately (it is abstract — provide a concrete subclass).
    /// </summary>
    public static IServiceCollection AddOutboxPoller<TMessage, TPoller>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TMessage : class
        where TPoller : class, IOutboxPoller<TMessage>
    {
        services.Configure<OutboxPollerOptions>(
            configuration.GetSection(OutboxPollerOptions.SectionName));
        services.TryAddScoped<IOutboxPoller<TMessage>, TPoller>();
        return services;
    }

    /// <summary>
    /// Registers the built-in <see cref="EfCoreOutboxPoller{TDbContext,TMessage}"/>
    /// which requires <c>IDbContextFactory&lt;TDbContext&gt;</c> to be registered.
    /// </summary>
    public static IServiceCollection AddEfCoreOutboxPoller<TDbContext, TMessage>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TDbContext : DbContext
        where TMessage : class, IOutboxMessage
    {
        services.Configure<OutboxPollerOptions>(
            configuration.GetSection(OutboxPollerOptions.SectionName));
        services.TryAddScoped<IOutboxPoller<TMessage>, EfCoreOutboxPoller<TDbContext, TMessage>>();
        return services;
    }

    // ── Eventual Consistency ──────────────────────────────────────────────────

    /// <summary>
    /// Registers the <see cref="EventualConsistencyStrategy{TEntity}"/> for the given entity type.
    /// Requires <c>ICacheService</c> (from <c>Usm.Shared.Caching</c>) to be registered.
    /// </summary>
    public static IServiceCollection AddEventualConsistency<TEntity>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TEntity : class
    {
        services.Configure<EventualConsistencyOptions>(
            configuration.GetSection(EventualConsistencyOptions.SectionName));
        services.TryAddScoped<IDatabaseScalingStrategy<TEntity>, EventualConsistencyStrategy<TEntity>>();
        return services;
    }

    // ── Materialized View ─────────────────────────────────────────────────────

    /// <summary>
    /// Registers the <see cref="MaterializedViewStrategy{TEntity}"/> for the given entity type.
    /// Requires <c>ICacheService</c> to be registered.
    /// </summary>
    public static IServiceCollection AddMaterializedViewCaching<TEntity>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MaterializedViewOptions<TEntity>>? configure = null)
        where TEntity : class
    {
        services.Configure<MaterializedViewOptions<TEntity>>(
            configuration.GetSection(MaterializedViewOptions<TEntity>.SectionName));

        if (configure is not null)
            services.PostConfigure(configure);

        services.TryAddScoped<IDatabaseScalingStrategy<TEntity>, MaterializedViewStrategy<TEntity>>();
        services.TryAddScoped<MaterializedViewStrategy<TEntity>>();
        return services;
    }

    // ── Row encryption ────────────────────────────────────────────────────────

    /// <summary>
    /// Registers the <see cref="RowEncryptionStrategy{TEntity}"/> for the given entity type.
    /// Configure the cipher and key via <c>appsettings.json</c> under
    /// <c>Database:RowEncryption</c>.
    /// </summary>
    public static IServiceCollection AddRowEncryption<TEntity>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TEntity : class
    {
        services.Configure<RowEncryptionOptions>(
            configuration.GetSection(RowEncryptionOptions.SectionName));
        services.TryAddScoped<IDatabaseScalingStrategy<TEntity>, RowEncryptionStrategy<TEntity>>();
        return services;
    }
}
