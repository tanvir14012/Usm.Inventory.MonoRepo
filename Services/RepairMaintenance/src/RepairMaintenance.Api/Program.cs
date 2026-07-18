using Microsoft.EntityFrameworkCore;
using RepairMaintenance.Application;
using RepairMaintenance.Infrastructure;
using RepairMaintenance.Infrastructure.Persistence;
using Usm.Shared.BuildingBlocks.Bootstrap;
using Usm.Shared.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args)
    .AddDefaultBootstrap();

builder.Services.AddRepairMaintenanceApplication();
builder.Services.AddRepairMaintenanceInfrastructure(builder.Configuration);
builder.Services.AddObservability(builder.Configuration, "RepairMaintenance.Api");
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_admin_dev");

var app = builder.Build()
    .UseDefaultMiddleware();

app.MapEndpoints();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { Service = "RepairMaintenance.Api", Status = "Up", Utc = DateTimeOffset.UtcNow }));

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RepairMaintenanceDbContext>();
    await dbContext.Database.MigrateAsync();
    await RepairMaintenanceDbSeeder.SeedAsync(dbContext);
}

app.Run();

public partial class Program;
