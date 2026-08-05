using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Patterns.Cache;
using Usm.Shared.Patterns.Cache.Abstractions;
using Usm.Shared.Patterns.Cache.Builders;

namespace Usm.Shared.Patterns.Cache.Extensions;

/// <summary>
/// Common extension methods for cache creation and DI registration.
/// </summary>
public static class CacheExtensions
{
    /// <summary>Registers the cache framework with dependency injection.</summary>
    public static IServiceCollection AddCacheFramework(this IServiceCollection services)
    {
        services.TryAddSingleton<CacheMetrics>();
        services.TryAddTransient(typeof(CacheBuilder<,>), typeof(CacheBuilder<,>));
        return services;
    }
}
