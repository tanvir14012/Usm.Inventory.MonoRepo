using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Usm.Shared.Caching.Extensions;
using Usm.Shared.Http.ResponseCaching.Extensions;

namespace Usm.Shared.BuildingBlocks.Bootstrap;

public static class BootstrapExtensions
{
    public static WebApplicationBuilder AddDefaultBootstrap(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
            .WriteTo.Console()
            .CreateLogger();

        builder.Host.UseSerilog();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        builder.Services.AddOpenApi();
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

        return app;
    }
}
