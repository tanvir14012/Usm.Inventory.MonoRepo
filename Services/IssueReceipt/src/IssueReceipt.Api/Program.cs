using IssueReceipt.Application;
using IssueReceipt.Infrastructure;
using Usm.Shared.BuildingBlocks.Bootstrap;
using Usm.Shared.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args)
    .AddDefaultBootstrap();

builder.Services.AddIssueReceiptApplication();
builder.Services.AddIssueReceiptInfrastructure(builder.Configuration);
builder.Services.AddObservability(builder.Configuration, "IssueReceipt.Api");
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_pass");

var app = builder.Build()
    .UseDefaultMiddleware();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { Service = "IssueReceipt.Api", Status = "Up", Utc = DateTimeOffset.UtcNow }));

app.Run();

public partial class Program;
