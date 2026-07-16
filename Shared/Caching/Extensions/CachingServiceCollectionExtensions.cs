using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Usm.Shared.Caching.Abstractions;
using Usm.Shared.Caching.Internal;
using Usm.Shared.Caching.Keys;
using Usm.Shared.Caching.Options;
using Usm.Shared.Caching.Serialization;
using Usm.Shared.Caching.Services;

namespace Usm.Shared.Caching.Extensions;

public static class CachingServiceCollectionExtensions
{
    public static IServiceCollection AddRedisCaching(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddOptions<RedisCachingOptions>();
        if (configuration is not null)
            services.Configure<RedisCachingOptions>(configuration.GetSection(RedisCachingOptions.SectionName));

        services.PostConfigure<RedisCachingOptions>(options =>
        {
            options.ConnectionString = string.IsNullOrWhiteSpace(options.ConnectionString)
                ? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? "localhost:6379"
                : options.ConnectionString;
            options.KeyPrefix = string.IsNullOrWhiteSpace(options.KeyPrefix) ? "usm" : options.KeyPrefix;
            options.InstanceName = string.IsNullOrWhiteSpace(options.InstanceName) ? "usm-cache" : options.InstanceName;
            options.DefaultAbsoluteExpirationSeconds = options.DefaultAbsoluteExpirationSeconds <= 0
                ? 300
                : options.DefaultAbsoluteExpirationSeconds;
            options.CompressionThresholdBytes = options.CompressionThresholdBytes <= 0
                ? 1024
                : options.CompressionThresholdBytes;
        });

        services.AddOptions<Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions>()
            .Configure<IOptions<RedisCachingOptions>>((redis, source) =>
            {
                redis.Configuration = source.Value.ConnectionString;
                redis.InstanceName = source.Value.InstanceName;
            });

        services.AddStackExchangeRedisCache(_ => { });
        services.AddMemoryCache();

        services.TryAddSingleton<ICacheSerializer, SystemTextJsonCacheSerializer>();
        services.TryAddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();
        services.TryAddSingleton<IRedisConnectionAccessor, RedisConnectionAccessor>();
        services.TryAddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}
