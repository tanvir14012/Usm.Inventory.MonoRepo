namespace Shared.AI.EngineClient;

using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Dependency injection extensions for the AI Engine client.
/// </summary>
public static class AiEngineServiceCollectionExtensions
{
    /// <summary>
    /// Registers the AI Engine gRPC client and resilience policies.
    /// </summary>
    public static IServiceCollection AddAiEngineClient(
        this IServiceCollection services,
        Uri endpoint,
        Action<AiEngineClientOptions>? configure = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (endpoint is null)
        {
            throw new ArgumentNullException(nameof(endpoint));
        }

        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<AiEngineClientOptions>();
        }

        services.AddGrpcClient<AIEngineService.AIEngineServiceClient>(client =>
            {
                client.Address = endpoint;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                EnableMultipleHttp2Connections = true
            })
            .AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromMilliseconds(200);
            });

        services.AddSingleton<IAIEngineClient, AiEngineClient>();
        return services;
    }
}

