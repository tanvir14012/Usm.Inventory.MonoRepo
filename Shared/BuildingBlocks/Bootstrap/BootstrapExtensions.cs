using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.RateLimiting;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using Usm.Shared.BuildingBlocks.Bootstrap.Middleware;
using Usm.Shared.BuildingBlocks.Bootstrap.Options;
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
        builder.Services.AddTransient<RequestTracingMiddleware>();
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

        // ── API Versioning ────────────────────────────────────────────────────
        // Reads ?api-version= query param and api-version header — matches the
        // Angular apiVersionInterceptor which sends both on every request.
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new QueryStringApiVersionReader("api-version"),
                new HeaderApiVersionReader("api-version"));
        });

        // ── Rate Limiting ─────────────────────────────────────────────────────
        // Global sliding-window + named policies. The Angular rateLimitInterceptor
        // reads the Retry-After header (delta-seconds) and auto-retries up to 2×.
        var rateLimitCfg = builder.Configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        builder.Services.Configure<RateLimitingOptions>(
            builder.Configuration.GetSection(RateLimitingOptions.SectionName));

        builder.Services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfterSeconds = rateLimitCfg.Global.WindowSeconds;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                }

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
                context.HttpContext.Response.ContentType = "application/problem+json";

                await context.HttpContext.Response.WriteAsync(
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        type = "https://tools.ietf.org/html/rfc6585#section-4",
                        title = "Too Many Requests",
                        status = 429,
                        detail = $"Rate limit exceeded. Retry after {retryAfterSeconds} second(s).",
                    }),
                    cancellationToken);
            };

            if (rateLimitCfg.Enabled)
            {
                // Global limiter — applies to every request automatically (no attribute needed).
                limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    context =>
                    {
                        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        return RateLimitPartition.GetSlidingWindowLimiter(ip, _ =>
                            new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = rateLimitCfg.Global.PermitLimit,
                                Window = TimeSpan.FromSeconds(rateLimitCfg.Global.WindowSeconds),
                                SegmentsPerWindow = 4,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = rateLimitCfg.Global.QueueLimit,
                            });
                    });
            }

            // Named policy: CDN upload endpoints — use .RequireRateLimiting(RateLimitPolicies.Upload)
            limiterOptions.AddFixedWindowLimiter(RateLimitPolicies.Upload, opts =>
            {
                opts.PermitLimit = rateLimitCfg.Upload.PermitLimit;
                opts.Window = TimeSpan.FromSeconds(rateLimitCfg.Upload.WindowSeconds);
                opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opts.QueueLimit = rateLimitCfg.Upload.QueueLimit;
            });

            // Named policy: auth / write-sensitive endpoints
            limiterOptions.AddFixedWindowLimiter(RateLimitPolicies.Strict, opts =>
            {
                opts.PermitLimit = rateLimitCfg.Strict.PermitLimit;
                opts.Window = TimeSpan.FromSeconds(rateLimitCfg.Strict.WindowSeconds);
                opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opts.QueueLimit = rateLimitCfg.Strict.QueueLimit;
            });
        });

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

        app.UseMiddleware<RequestTracingMiddleware>();

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
                    detail = app.Environment.IsDevelopment() ? ex?.Message : "Internal server error.",
                    traceId = context.Response.Headers[RequestTracingMiddleware.TraceIdHeaderName].ToString()
                };
                var json = System.Text.Json.JsonSerializer.Serialize(
                    problem,
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
                await context.Response.WriteAsync(json);
            });
        });

        app.UseSerilogRequestLogging();
        app.UseCors();
        app.UseRateLimiter();   // 429 + Retry-After before auth — protects all endpoints
        app.UseHttpResponseCaching();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<AuditLoggingMiddleware>();

        return app;
    }
}
