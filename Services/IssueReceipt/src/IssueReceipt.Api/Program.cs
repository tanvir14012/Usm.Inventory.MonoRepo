using IssueReceipt.Application;
using IssueReceipt.Infrastructure;
using IssueReceipt.Infrastructure.Persistence;
using Usm.Shared.BuildingBlocks.Bootstrap;
using Usm.Shared.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args)
    .AddDefaultBootstrap();

builder.Services.AddIssueReceiptApplication();
builder.Services.AddIssueReceiptInfrastructure(builder.Configuration);
builder.Services.AddObservability(builder.Configuration, "IssueReceipt.Api");
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_admin_dev");

var app = builder.Build()
    .UseDefaultMiddleware();

app.MapEndpoints();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { Service = "IssueReceipt.Api", Status = "Up", Utc = DateTimeOffset.UtcNow }));

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IssueReceiptDbContext>();
    await IssueReceiptDbSeeder.SeedAsync(dbContext);
}

app.Run();

public partial class Program;
