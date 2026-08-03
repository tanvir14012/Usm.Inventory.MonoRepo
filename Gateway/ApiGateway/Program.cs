using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Usm.Inventory.MonoRepo.ServiceDefaults;
using Usm.Shared.BuildingBlocks.Bootstrap;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDotEnvFile(builder.Environment.ContentRootPath);

builder.AddServiceDefaults();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Gateway-level rate limiting: first line of defence before proxying.
// Keyed by client IP — applies to every route on the gateway.
const int gatewayPermitLimit = 500;
const int gatewayWindowSeconds = 60;

builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers["Retry-After"] = gatewayWindowSeconds.ToString();
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "https://tools.ietf.org/html/rfc6585#section-4",
                title = "Too Many Requests",
                status = 429,
                detail = $"Gateway rate limit exceeded. Retry after {gatewayWindowSeconds} second(s).",
            }),
            cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetSlidingWindowLimiter(ip, _ =>
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = gatewayPermitLimit,
                Window = TimeSpan.FromSeconds(gatewayWindowSeconds),
                SegmentsPerWindow = 4,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 20,
            });
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseRateLimiter();
app.MapReverseProxy();

app.Run();