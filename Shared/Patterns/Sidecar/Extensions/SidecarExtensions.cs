using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.Sidecar.Abstractions;
using Usm.Shared.Patterns.Sidecar.Builders;
using Usm.Shared.Patterns.Sidecar.Models;

namespace Usm.Shared.Patterns.Sidecar.Extensions;

/// <summary>
/// Dependency injection extensions for the sidecar pattern.
/// </summary>
public static class SidecarExtensions
{
    /// <summary>
    /// Registers the sidecar framework core services (builder, options).
    /// </summary>
    public static IServiceCollection AddSidecarFramework(this IServiceCollection services)
    {
        services.AddOptions<SidecarOptions>();
        services.TryAddTransient(typeof(SidecarBuilder<>), typeof(SidecarBuilder<>));
        services.TryAddTransient(typeof(ISidecarBuilder<>), typeof(SidecarBuilder<>));
        return services;
    }

    /// <summary>
    /// Registers a named <see cref="ISidecar{TService}"/> as a singleton, wrapping
    /// <typeparamref name="TImplementation"/> as the primary.
    /// </summary>
    /// <remarks>
    /// To add health monitoring, call
    /// <c>services.AddHealthChecks().AddCheck&lt;SidecarHealthCheck&lt;TService&gt;&gt;("my-sidecar")</c>
    /// after this registration.
    /// </remarks>
    /// <typeparam name="TService">Primary service interface.</typeparam>
    /// <typeparam name="TImplementation">Concrete primary implementation.</typeparam>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configure">Optional delegate to customise the sidecar options.</param>
    public static IServiceCollection AddSidecar<TService, TImplementation>(
        this IServiceCollection services,
        Action<SidecarOptions>? configure = null)
        where TService : class
        where TImplementation : class, TService
    {
        services.TryAddSingleton<TImplementation>();

        if (configure is not null)
            services.Configure<SidecarOptions>(configure);

        services.TryAddSingleton<ISidecar<TService>>(sp =>
        {
            var primary = sp.GetRequiredService<TImplementation>();
            var options = sp.GetRequiredService<IOptions<SidecarOptions>>();
            var logger  = sp.GetService<ILogger<Sidecar<TService>>>();
            return new Sidecar<TService>(primary, options, logger);
        });

        // Register the health check type so the caller can wire it via AddHealthChecks()
        services.TryAddTransient<SidecarHealthCheck<TService>>();

        return services;
    }
}

/// <summary>
/// ASP.NET Core health check that reports the circuit state of a sidecar instance.
/// </summary>
/// <typeparam name="TService">The primary service contract.</typeparam>
public sealed class SidecarHealthCheck<TService> : IHealthCheck where TService : class
{
    private readonly ISidecar<TService> _sidecar;

    /// <summary>Initializes a new health check.</summary>
    public SidecarHealthCheck(ISidecar<TService> sidecar)
        => _sidecar = sidecar ?? throw new ArgumentNullException(nameof(sidecar));

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var snapshot = _sidecar.Metrics.Snapshot(_sidecar.CircuitState);

        var data = new Dictionary<string, object>
        {
            ["circuitState"] = snapshot.CircuitState.ToString(),
            ["totalCalls"]   = snapshot.TotalCalls,
            ["successes"]    = snapshot.Successes,
            ["failures"]     = snapshot.Failures,
            ["retries"]      = snapshot.Retries,
            ["timeouts"]     = snapshot.Timeouts,
            ["circuitTrips"] = snapshot.CircuitTrips
        };

        var result = snapshot.CircuitState switch
        {
            SidecarCircuitState.Closed =>
                new HealthCheckResult(HealthStatus.Healthy, "Circuit closed — operating normally.", data: data),

            SidecarCircuitState.HalfOpen =>
                new HealthCheckResult(HealthStatus.Degraded, "Circuit half-open — probing recovery.", data: data),

            SidecarCircuitState.Open =>
                new HealthCheckResult(HealthStatus.Unhealthy, "Circuit open — calls are being rejected.", data: data),

            _ => new HealthCheckResult(HealthStatus.Degraded, "Unknown circuit state.", data: data)
        };

        return Task.FromResult(result);
    }
}
