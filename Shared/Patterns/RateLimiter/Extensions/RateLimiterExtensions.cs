using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Patterns.RateLimiter.Abstractions;
using Usm.Shared.Patterns.RateLimiter.Builders;

namespace Usm.Shared.Patterns.RateLimiter.Extensions;

/// <summary>
/// Common extension methods for rate limiter registration.
/// </summary>
public static class RateLimiterExtensions
{
    /// <summary>Registers the rate limiter framework with dependency injection.</summary>
    public static IServiceCollection AddRateLimiterFramework(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(IRateLimiterBuilder<>), typeof(RateLimiterBuilder<>));
        return services;
    }
}
