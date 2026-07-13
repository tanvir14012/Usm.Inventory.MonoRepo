using Salvage.Application;
using Salvage.Infrastructure;
using Usm.Shared.BuildingBlocks.Bootstrap;
using Usm.Shared.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args)
    .AddDefaultBootstrap();

builder.Services.AddSalvageApplication();
builder.Services.AddSalvageInfrastructure(builder.Configuration);
builder.Services.AddObservability(builder.Configuration, "Salvage.Api");
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_pass");

var app = builder.Build()
    .UseDefaultMiddleware();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { Service = "Salvage.Api", Status = "Up", Utc = DateTimeOffset.UtcNow }));

app.Run();

public partial class Program;
