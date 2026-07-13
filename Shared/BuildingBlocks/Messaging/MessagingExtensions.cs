using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Usm.Shared.BuildingBlocks.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var host = configuration["RabbitMq:Host"] ?? "localhost";
        var username = configuration["RabbitMq:Username"] ?? "guest";
        var password = configuration["RabbitMq:Password"] ?? "guest";

        services.AddMassTransit(configure =>
        {
            configure.SetKebabCaseEndpointNameFormatter();
            configure.UsingRabbitMq((context, bus) =>
            {
                bus.Host(host, "/", hostConfig =>
                {
                    hostConfig.Username(username);
                    hostConfig.Password(password);
                });
                bus.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
