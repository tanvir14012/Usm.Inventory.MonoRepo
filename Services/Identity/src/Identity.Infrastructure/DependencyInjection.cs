using Identity.Infrastructure.Persistence;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using System.Security.Cryptography.X509Certificates;
using Usm.Shared.BuildingBlocks.Localization;
using Usm.Shared.BuildingBlocks.Messaging;
using Usm.Shared.BuildingBlocks.Persistence.Migrations;
using Usm.Shared.Data.DbContextExtensions;
using static Usm.Shared.Reflection.AssemblyScanning.AssemblyScanningExtensions;
using Fido2NetLib;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_pass";

        services.AddServiceDbContext<IdentityDbContext>(connectionString, "identity");
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        var signingCertificate = X509CertificateLoader.LoadPkcs12FromFile(
            path: configuration["OpenIddict:CertificatePath"]!,
            password: configuration["OpenIddict:CertificatePassword"],
            keyStorageFlags: X509KeyStorageFlags.EphemeralKeySet);

        services.AddOpenIddict()

            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<IdentityDbContext>()
                       .ReplaceDefaultEntities<Guid>();
            })
            .AddServer(options =>
            {
                // Endpoints
                options.SetAuthorizationEndpointUris("/connect/authorize");
                options.SetTokenEndpointUris("/connect/token");
                options.SetEndSessionEndpointUris("/connect/logout");
                options.SetUserInfoEndpointUris("/connect/userinfo");

                // OAuth flows
                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange();

                options.AllowRefreshTokenFlow();

                // Scopes
                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.OfflineAccess);


                options.AddSigningCertificate(signingCertificate);

                options.AddEncryptionKey(
                    new SymmetricSecurityKey(
                        Convert.FromHexString(
                            configuration["OpenIddict:EncryptionKey"]!)));

                // ASP.NET Core integration
                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough();

                // Token lifetimes
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(30));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

            })
            .AddValidation(options =>
            {
                // Import the configuration from the local OpenIddict server instance.
                options.UseLocalServer();

                // Register the ASP.NET Core host.
                options.UseAspNetCore();
            });

        services.AddRabbitMqMessaging(configuration);
        services.AddResxLocalization();
        services.AddAutoMigrations<IdentityDbContext>();

        services.AddFido2(options =>
        {
            options.ServerDomain = configuration["Fido2:RpId"]!;
            options.ServerName = configuration["Fido2:RpName"]!;
            options.Origins.Append(configuration["Fido2:Origin"]!);
        });

        services
            .AddScopedServices(typeof(DependencyInjection).Assembly)
            .AddTransientServices(typeof(DependencyInjection).Assembly)
            .AddSingletonServices(typeof(DependencyInjection).Assembly);

        return services;
    }
}
