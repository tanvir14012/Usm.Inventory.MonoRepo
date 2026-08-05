using System.Text.Json;
using Shared.AI.EngineClient;

var builder = WebApplication.CreateBuilder(args);

var endpoint = new Uri(Environment.GetEnvironmentVariable("AI_ENGINE_ENDPOINT") ?? "http://python-ai-engine:50051");
builder.Services.AddAiEngineClient(endpoint, options =>
{
    options.Timeout = TimeSpan.FromSeconds(90);
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/tasks/execute", async (IAIEngineClient client, ExecuteTaskRequest request, CancellationToken ct) =>
{
    var result = await client.ExecuteAsync<JsonElement?>(request.TaskType, request.Payload, ct);
    return Results.Ok(result);
});

app.MapPost("/tasks/stream", async (IAIEngineClient client, StreamTaskRequest request, CancellationToken ct) =>
{
    var results = new List<string>();
    await foreach (var item in client.StreamAsync<string>(request.TaskType, request.Payload, ct))
    {
        results.Add(item);
    }

    return Results.Ok(results);
});

app.Run();

public sealed record ExecuteTaskRequest(string TaskType, JsonElement Payload);
public sealed record StreamTaskRequest(string TaskType, JsonElement Payload);
