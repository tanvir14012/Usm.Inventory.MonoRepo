using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using Usm.Shared.BuildingBlocks.Bootstrap.Middleware;
using Usm.Shared.Caching.Extensions;
using Usm.Shared.Http.ResponseCaching.Extensions;

namespace Usm.Shared.BuildingBlocks.Bootstrap;

public static class BootstrapExtensions
{
    public static WebApplicationBuilder AddDefaultBootstrap(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddDotEnvFile(builder.Environment.ContentRootPath);
        NormalizePostgresConnectionString(builder.Configuration);

        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
            .WriteTo.Console();

        var lokiEndpoint = builder.Configuration["Observability:LokiEndpoint"];
        if (string.IsNullOrWhiteSpace(lokiEndpoint) && !builder.Environment.IsDevelopment())
        {
            lokiEndpoint = "http://loki:3100";
        }

        if (!string.IsNullOrWhiteSpace(lokiEndpoint))
        {
            loggerConfiguration.WriteTo.GrafanaLoki(
                lokiEndpoint,
                [new LokiLabel { Key = "application", Value = builder.Environment.ApplicationName }]);
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        builder.Host.UseSerilog();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        builder.Services.AddOpenApi();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<AuditLoggingMiddleware>();
        builder.Services.AddRedisCaching(builder.Configuration);
        builder.Services.AddHttpResponseCaching(builder.Configuration);

        builder.Services
            .AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = builder.Configuration["Jwt:Authority"];
                options.Audience  = builder.Configuration["Jwt:Audience"];
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            });

        builder.Services.AddAuthorization();

        return builder;
    }

    private static void NormalizePostgresConnectionString(ConfigurationManager configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres");
        var usmDbConnectionString = configuration.GetConnectionString("usmdb");
        var postgresConnectionStringFromEnv = configuration["POSTGRES_CONNECTION_STRING"];

        if ((string.IsNullOrWhiteSpace(postgresConnectionString) || IsMaskedPlaceholder(postgresConnectionString))
            && !string.IsNullOrWhiteSpace(postgresConnectionStringFromEnv))
        {
            configuration["ConnectionStrings:Postgres"] = postgresConnectionStringFromEnv;
            postgresConnectionString = postgresConnectionStringFromEnv;
        }

        if ((string.IsNullOrWhiteSpace(postgresConnectionString) || IsMaskedPlaceholder(postgresConnectionString))
            && !string.IsNullOrWhiteSpace(usmDbConnectionString))
        {
            configuration["ConnectionStrings:Postgres"] = usmDbConnectionString;
            postgresConnectionString = usmDbConnectionString;
        }

        if ((string.IsNullOrWhiteSpace(usmDbConnectionString) || IsMaskedPlaceholder(usmDbConnectionString))
            && !string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            configuration["ConnectionStrings:usmdb"] = postgresConnectionString;
        }
    }

    private static bool IsMaskedPlaceholder(string value)
        => value.Contains("******", StringComparison.Ordinal);

    public static WebApplication UseDefaultMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var ex = feature?.Error;
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/problem+json";
                var problem = new
                {
                    type   = "https://tools.ietf.org/html/rfc7807",
                    title  = "An unexpected error occurred.",
                    status = 500,
                    detail = app.Environment.IsDevelopment() ? ex?.Message : "Internal server error."
                };
                var json = System.Text.Json.JsonSerializer.Serialize(
                    problem,
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
                await context.Response.WriteAsync(json);
            });
        });

        app.UseSerilogRequestLogging();
        app.UseCors();
        app.UseHttpResponseCaching();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<AuditLoggingMiddleware>();

        return app;
    }
}
