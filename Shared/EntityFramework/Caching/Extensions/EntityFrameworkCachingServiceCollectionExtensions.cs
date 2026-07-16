using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.EntityFramework.Caching.Interceptors;
using Usm.Shared.EntityFramework.Caching.Options;

namespace Usm.Shared.EntityFramework.Caching.Extensions;

public static class EntityFrameworkCachingServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFrameworkCaching(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddOptions<EntityFrameworkCachingOptions>();
        if (configuration is not null)
            services.Configure<EntityFrameworkCachingOptions>(configuration.GetSection(EntityFrameworkCachingOptions.SectionName));

        services.PostConfigure<EntityFrameworkCachingOptions>(options =>
        {
            options.KeyNamespace = string.IsNullOrWhiteSpace(options.KeyNamespace) ? "ef" : options.KeyNamespace;
            options.DefaultExpirationSeconds = options.DefaultExpirationSeconds <= 0 ? 300 : options.DefaultExpirationSeconds;
        });

        services.TryAddScoped<EntityFrameworkCacheInvalidationInterceptor>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IInterceptor, EntityFrameworkCacheInvalidationInterceptor>());

        return services;
    }
}
