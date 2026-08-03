using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Usm.Shared.BuildingBlocks.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var host = configuration["RabbitMq:Host"] ?? "localhost";
        var virtualHost = configuration["RabbitMq:VirtualHost"] ?? "/";
        var username = configuration["RabbitMq:Username"] ?? "guest";
        var password = configuration["RabbitMq:Password"] ?? "guest";
        var port = ushort.TryParse(configuration["RabbitMq:Port"], out var configuredPort) ? configuredPort : (ushort)5672;

        var connectionString = configuration.GetConnectionString("rabbitmq")
            ?? configuration.GetConnectionString("RabbitMq")
            ?? configuration["RabbitMq:ConnectionString"];

        if (TryParseConnectionString(connectionString, out var parsed))
        {
            host = parsed.Host;
            port = parsed.Port;
            virtualHost = parsed.VirtualHost;
            username = parsed.Username;
            password = parsed.Password;
        }

        services.AddMassTransit(configure =>
        {
            configure.SetKebabCaseEndpointNameFormatter();
            configure.UsingRabbitMq((context, bus) =>
            {
                bus.Host(host, port, virtualHost, hostConfig =>
                {
                    hostConfig.Username(username);
                    hostConfig.Password(password);
                });
                bus.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static bool TryParseConnectionString(string? connectionString, out RabbitMqSettings settings)
    {
        settings = default;
        if (string.IsNullOrWhiteSpace(connectionString) || !Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, "amqp", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, "amqps", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var virtualHost = uri.AbsolutePath.Trim('/');
        var userInfo = uri.UserInfo.Split(':', 2, StringSplitOptions.TrimEntries);

        settings = new RabbitMqSettings(
            Host: uri.Host,
            Port: uri.IsDefaultPort ? (ushort)(string.Equals(uri.Scheme, "amqps", StringComparison.OrdinalIgnoreCase) ? 5671 : 5672) : (ushort)uri.Port,
            VirtualHost: string.IsNullOrWhiteSpace(virtualHost) ? "/" : Uri.UnescapeDataString(virtualHost),
            Username: userInfo.Length > 0 && !string.IsNullOrWhiteSpace(userInfo[0]) ? Uri.UnescapeDataString(userInfo[0]) : "guest",
            Password: userInfo.Length > 1 && !string.IsNullOrWhiteSpace(userInfo[1]) ? Uri.UnescapeDataString(userInfo[1]) : "guest");
        return true;
    }

    private readonly record struct RabbitMqSettings(
        string Host,
        ushort Port,
        string VirtualHost,
        string Username,
        string Password);
}
