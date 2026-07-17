using TrafficSecurity.Application;
using TrafficSecurity.Infrastructure;
using TrafficSecurity.Infrastructure.Persistence;
using Usm.Shared.BuildingBlocks.Bootstrap;
using Usm.Shared.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args)
    .AddDefaultBootstrap();

builder.Services.AddTrafficSecurityApplication();
builder.Services.AddTrafficSecurityInfrastructure(builder.Configuration);
builder.Services.AddObservability(builder.Configuration, "TrafficSecurity.Api");
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_pass");

var app = builder.Build()
    .UseDefaultMiddleware();

app.MapEndpoints();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { Service = "TrafficSecurity.Api", Status = "Up", Utc = DateTimeOffset.UtcNow }));

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TrafficSecurityDbContext>();
    await TrafficSecurityDbSeeder.SeedAsync(dbContext);
}

app.Run();

public partial class Program;
