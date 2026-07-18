using Communication.Application;
using Communication.Infrastructure;
using Communication.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.BuildingBlocks.Bootstrap;
using Usm.Shared.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args)
    .AddDefaultBootstrap();

builder.Services.AddCommunicationApplication();
builder.Services.AddCommunicationInfrastructure(builder.Configuration);
builder.Services.AddObservability(builder.Configuration, "Communication.Api");
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_admin_dev");

var app = builder.Build()
    .UseDefaultMiddleware();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { Service = "Communication.Api", Status = "Up", Utc = DateTimeOffset.UtcNow }));

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();

public partial class Program;
