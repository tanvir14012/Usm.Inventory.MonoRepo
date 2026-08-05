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

app.MapPost("/demo/embedding", async (TransformersWrapper wrapper, DemoTextRequest request, CancellationToken ct) =>
{
    var embedding = await wrapper.GetEmbeddingAsync(request.Text, request.Model, ct);
    return Results.Ok(new { size = embedding.Length, vector = embedding });
});

app.MapPost("/demo/entities", async (spaCyWrapper wrapper, DemoTextRequest request, CancellationToken ct) =>
{
    var entities = await wrapper.ExtractEntitiesAsync(request.Text, request.Model, ct);
    return Results.Ok(entities);
});

app.Run();

public sealed record DemoTextRequest(string Text, string Model);
