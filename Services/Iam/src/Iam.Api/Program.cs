using Iam.Application;
using Iam.Infrastructure;
using Usm.Shared.BuildingBlocks.Bootstrap;
using Usm.Shared.BuildingBlocks.Observability;
using static Usm.Shared.Reflection.AssemblyScanning.AssemblyScanningExtensions;

var builder = WebApplication.CreateBuilder(args)
    .AddDefaultBootstrap();

builder.Services.AddIamApplication();
builder.Services.AddIamInfrastructure(builder.Configuration);
builder.Services.AddObservability(builder.Configuration, "Iam.Api");
builder.Services
    .AddScopedServices(typeof(Program).Assembly)
    .AddTransientServices(typeof(Program).Assembly)
    .AddSingletonServices(typeof(Program).Assembly);
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_pass");

var app = builder.Build()
    .UseDefaultMiddleware();

app.MapEndpoints();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new
{
    Service = "Iam.Api",
    Status = "Up",
    Utc = DateTimeOffset.UtcNow
}));

app.Run();

public partial class Program;
