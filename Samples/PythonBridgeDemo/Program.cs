using Shared.AI.Extensions;
using Shared.AI.Python;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPythonAI(builder.Configuration, sectionName: "PythonAI");

var app = builder.Build();

await app.Services.GetRequiredService<PersistentPythonBridge>().InitializeAsync();

app.MapGet("/health", (IPythonProcessManager bridge) =>
{
    var snapshot = bridge.GetSnapshot();
    return Results.Ok(new
    {
        started = snapshot.Started,
        healthyWorkers = snapshot.HealthyWorkers,
        busyWorkers = snapshot.BusyWorkers,
        queuedRequests = snapshot.QueuedRequests,
        lastError = snapshot.LastError
    });
});

app.Run();
