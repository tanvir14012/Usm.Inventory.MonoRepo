using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Usm.Shared.Infrastructure.CDN.Abstractions;
using Usm.Shared.Infrastructure.CDN.Cache;
using Usm.Shared.Infrastructure.CDN.Lifecycle;
using Usm.Shared.Infrastructure.CDN.Media;
using Usm.Shared.Infrastructure.CDN.Options;
using Usm.Shared.Infrastructure.CDN.Security;
using Usm.Shared.Infrastructure.CDN.Storage;
using Usm.Shared.Infrastructure.CDN.Strategies;

namespace Usm.Shared.Infrastructure.CDN.Extensions;

/// <summary>
/// Registers all CDN infrastructure services into the DI container.
///
/// Usage (Program.cs / Startup):
/// <code>
///   builder.Services.AddCdnInfrastructure(builder.Configuration);
///   // or with inline override:
///   builder.Services.AddCdnInfrastructure(builder.Configuration, cdn =>
///   {
///       cdn.SecureLink.SecretKey = "my-secret";
///       cdn.EnableEdgeProcessing = true;
///   });
/// </code>
///
/// Prerequisites:
///   • Call <c>services.AddRedisCaching(configuration)</c> from Usm.Shared.Caching before this method,
///     or supply a <c>CDN:RedisConnectionString</c> configuration value.
/// </summary>
public static class CdnServiceCollectionExtensions
{
    public static IServiceCollection AddCdnInfrastructure(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<CdnOptions>? configure = null)
    {
        // ── Options ──────────────────────────────────────────────────────────
        services.AddOptions<CdnOptions>();

        if (configuration is not null)
            services.Configure<CdnOptions>(configuration.GetSection(CdnOptions.SectionName));

        if (configure is not null)
            services.Configure(configure);

        // ── Storage providers (factory-created per options at first resolve) ─
        // Returns a single IReadOnlyList containing ALL providers; later registrations
        // expose IEnumerable<IStorageProvider> from this same list.
        services.TryAddSingleton<IReadOnlyList<IStorageProvider>>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CdnOptions>>().Value;
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            return opts.StorageProviders
                .Select<StorageProviderOptions, IStorageProvider>(p => p.Type switch
                {
                    StorageProviderType.LocalFileSystem =>
                        new LocalFileSystemStorageProvider(
                            p, loggerFactory.CreateLogger<LocalFileSystemStorageProvider>()),
                    StorageProviderType.AzureBlob =>
                        throw new NotSupportedException(
                            "AzureBlob storage type requires the Azure.Storage.Blobs package. " +
                            "Implement a custom IStorageProvider and register it before calling AddCdnInfrastructure."),
                    _ => // S3Compatible (default)
                        new S3CompatibleStorageProvider(
                            p, loggerFactory.CreateLogger<S3CompatibleStorageProvider>())
                })
                .ToList()
                .AsReadOnly();
        });

        services.TryAddSingleton<IEnumerable<IStorageProvider>>(
            sp => sp.GetRequiredService<IReadOnlyList<IStorageProvider>>());

        // ── Core engine & security ───────────────────────────────────────────
        services.TryAddSingleton<IStorageProviderEngine, StorageProviderEngine>();
        services.TryAddSingleton<INginxSecureLinkGenerator, NginxSecureLinkGenerator>();
        services.TryAddSingleton<ISecureUploadHandler, SecureUploadHandler>();

        // ── Media processors ─────────────────────────────────────────────────
        services.TryAddSingleton<IMediaProcessor, AdaptiveImageProcessor>();
        services.TryAddSingleton<ByteRangeStreamingHandler>();
        services.TryAddSingleton<HlsFragmentProcessor>();

        // ── Cache layer ───────────────────────────────────────────────────────
        services.TryAddSingleton<AssetCacheManager>();
        services.TryAddSingleton<ICdnCacheInvalidator, CdnCacheInvalidator>();

        // ── Redis IConnectionMultiplexer (needed for pub/sub invalidation) ───
        // Registers only if not already in the container (e.g. provided by Usm.Shared.Caching).
        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var cdnOpts = sp.GetRequiredService<IOptions<CdnOptions>>().Value;
            var connectionString = cdnOpts.RedisConnectionString
                ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
                ?? "localhost:6379";

            return ConnectionMultiplexer.Connect(connectionString);
        });

        // ── Distribution strategies ───────────────────────────────────────────
        // Use TryAddEnumerable so multiple calls to AddCdnInfrastructure don't duplicate
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICdnDistributionStrategy, EdgeProcessingStrategy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICdnDistributionStrategy, OriginShieldStrategy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICdnDistributionStrategy, RegionalShardingStrategy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICdnDistributionStrategy, LoadDistributionStrategy>());

        services.TryAddSingleton<CdnDistributionOrchestrator>();

        // ── Lifecycle initializer (singleton shared between IHostedService and interface) ─
        services.AddSingleton<EdgeAssetInitializerService>();
        services.TryAddSingleton<IEdgeAssetInitializer>(
            sp => sp.GetRequiredService<EdgeAssetInitializerService>());
        services.AddHostedService(
            sp => sp.GetRequiredService<EdgeAssetInitializerService>());

        return services;
    }
}
