using Reporting.Application;
using Reporting.Infrastructure;
using Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.BuildingBlocks.Bootstrap;
using Usm.Shared.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args)
    .AddDefaultBootstrap();

builder.Services.AddReportingApplication();
builder.Services.AddReportingInfrastructure(builder.Configuration);
builder.Services.AddObservability(builder.Configuration, "Reporting.Api");
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_admin_dev");

var app = builder.Build()
    .UseDefaultMiddleware();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { Service = "Reporting.Api", Status = "Up", Utc = DateTimeOffset.UtcNow }));

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
public partial class Program;
