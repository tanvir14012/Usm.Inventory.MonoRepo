using Usm.Inventory.MonoRepo.ServiceDefaults;
using Usm.Shared.BuildingBlocks.Bootstrap;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDotEnvFile(builder.Environment.ContentRootPath);

builder.AddServiceDefaults();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapReverseProxy();

app.Run();