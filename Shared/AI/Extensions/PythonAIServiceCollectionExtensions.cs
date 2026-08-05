namespace Shared.AI.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.AI.Python;

/// <summary>
/// Dependency injection extensions for the Python AI runtime.
/// </summary>
public static class PythonAIServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Python AI runtime with options binding and hosted startup.
    /// </summary>
    public static IServiceCollection AddPythonAI(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "PythonAI")
    {
        services.AddOptions<PythonAIOptions>()
            .BindConfiguration(sectionName)
            .Validate(options => options.Pools.Count == 0 || options.Pools.All(pool => pool.WorkerCount > 0), "Each Python worker pool must contain at least one worker.")
            .ValidateOnStart();

        services.AddSingleton<PersistentPythonBridge>();
        services.AddSingleton<IPythonProcessManager>(sp => sp.GetRequiredService<PersistentPythonBridge>());
        services.AddSingleton<IHostedService, PythonAIHostedService>();
        services.AddSingleton<PythonAIHealthCheck>();
        services.AddSingleton<TransformersWrapper>();
        services.AddSingleton<spaCyWrapper>();

        return services;
    }

    /// <summary>
    /// Adds the Python AI runtime with an options callback.
    /// </summary>
    public static IServiceCollection AddPythonAI(
        this IServiceCollection services,
        Action<PythonAIOptions> configure)
    {
        services.AddOptions<PythonAIOptions>().Configure(configure).ValidateOnStart();

        services.AddSingleton<PersistentPythonBridge>();
        services.AddSingleton<IPythonProcessManager>(sp => sp.GetRequiredService<PersistentPythonBridge>());
        services.AddSingleton<IHostedService, PythonAIHostedService>();
        services.AddSingleton<PythonAIHealthCheck>();
        services.AddSingleton<TransformersWrapper>();
        services.AddSingleton<spaCyWrapper>();

        return services;
    }

}
