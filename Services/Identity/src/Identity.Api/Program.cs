using Identity.Application;
using Identity.Infrastructure;
using Usm.Shared.BuildingBlocks.Bootstrap;
using Usm.Shared.BuildingBlocks.Observability;
using static Usm.Shared.Reflection.AssemblyScanning.AssemblyScanningExtensions;

var builder = WebApplication.CreateBuilder(args)
    .AddDefaultBootstrap();

builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddObservability(builder.Configuration, "Identity.Api");
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_admin_dev");

builder.Services
    .AddScopedServices(typeof(Program).Assembly)
    .AddTransientServices(typeof(Program).Assembly)
    .AddSingletonServices(typeof(Program).Assembly);

builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build()
    .UseDefaultMiddleware();

app.MapEndpoints();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new
{
    Service = "Identity.Api",
    Status = "Up",
    Utc = DateTimeOffset.UtcNow
}));

app.Run();

public partial class Program;
