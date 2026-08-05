namespace Shared.AI.Python;

using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Identifies the worker pool a request should run on.
/// </summary>
public enum PythonWorkerRole
{
    /// <summary>General CPU-bound inference.</summary>
    Cpu,

    /// <summary>GPU-accelerated inference.</summary>
    Gpu,

    /// <summary>NLP-heavy workloads.</summary>
    Nlp,

    /// <summary>Computer vision workloads.</summary>
    Vision,

    /// <summary>Fallback pool for custom operations.</summary>
    Generic
}

/// <summary>
/// Scheduling strategy used by the worker pool.
/// </summary>
public enum PythonWorkerScheduling
{
    /// <summary>Prefer the least busy worker.</summary>
    LeastBusy,

    /// <summary>Rotate workers in order.</summary>
    RoundRobin
}

/// <summary>
/// Default request operation names.
/// </summary>
public static class PythonOperations
{
    /// <summary>Embedding generation.</summary>
    public const string Embedding = "embedding";

    /// <summary>Batch embedding generation.</summary>
    public const string Embeddings = "embeddings";

    /// <summary>Sentiment analysis.</summary>
    public const string Sentiment = "sentiment";

    /// <summary>Named entity recognition.</summary>
    public const string Ner = "ner";

    /// <summary>Summarization.</summary>
    public const string Summarization = "summarization";

    /// <summary>Translation.</summary>
    public const string Translation = "translation";

    /// <summary>Classification.</summary>
    public const string Classification = "classification";

    /// <summary>Custom function invocation.</summary>
    public const string Invoke = "invoke";

    /// <summary>Heartbeat check.</summary>
    public const string Heartbeat = "heartbeat";

    /// <summary>Shutdown signal.</summary>
    public const string Shutdown = "shutdown";
}

/// <summary>
/// Root configuration for the Python AI runtime.
/// </summary>
public sealed class PythonAIOptions
{
    /// <summary>Gets or sets the Python executable path.</summary>
    public string? PythonExecutablePath { get; set; }

    /// <summary>Gets or sets the virtual environment directory.</summary>
    public string? VirtualEnvironmentPath { get; set; }

    /// <summary>Gets or sets the worker bootstrap module.</summary>
    public string WorkerModule { get; set; } = "usm_shared_ai.worker";

    /// <summary>Gets or sets the worker script path copied beside the assembly.</summary>
    public string WorkerScriptPath { get; set; } = Path.Combine("python_worker", "worker.py");

    /// <summary>Gets or sets the root folder containing the Python package.</summary>
    public string? WorkerRootPath { get; set; }

    /// <summary>Gets or sets the worker pool definitions.</summary>
    public List<PythonWorkerPoolOptions> Pools { get; set; } = new();

    /// <summary>Gets or sets the worker startup timeout.</summary>
    public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(90);

    /// <summary>Gets or sets the per-request timeout.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>Gets or sets the heartbeat interval.</summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>Gets or sets the restart delay after a crash.</summary>
    public TimeSpan RestartDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets the maximum restart attempts before circuit breaking.</summary>
    public int MaxRestartAttempts { get; set; } = 10;

    /// <summary>Gets or sets the maximum request retries across workers.</summary>
    public int MaxRequestRetries { get; set; } = 2;

    /// <summary>Gets or sets the scheduling policy.</summary>
    public PythonWorkerScheduling Scheduling { get; set; } = PythonWorkerScheduling.LeastBusy;

    /// <summary>Gets or sets the JSON protocol version.</summary>
    public int ProtocolVersion { get; set; } = 1;

    /// <summary>Gets or sets a value indicating whether warm-up requests run on startup.</summary>
    public bool WarmupModels { get; set; } = true;

    /// <summary>Gets or sets the custom function registrations.</summary>
    public List<PythonCustomFunctionOptions> CustomFunctions { get; set; } = new();

    /// <summary>Gets or sets the operation allow list. Empty means all registered operations are allowed.</summary>
    public List<string> AllowedOperations { get; set; } = new();

    /// <summary>Gets or sets the minimum supported Python version.</summary>
    public Version MinimumPythonVersion { get; set; } = new(3, 10);

    /// <summary>Creates a sensible default configuration.</summary>
    public static PythonAIOptions CreateDefault()
    {
        return new PythonAIOptions
        {
            Pools =
            {
                new PythonWorkerPoolOptions
                {
                    Name = "cpu",
                    Role = PythonWorkerRole.Cpu,
                    WorkerCount = Math.Max(1, Environment.ProcessorCount / 2),
                    MaxConcurrentRequestsPerWorker = 4,
                    Models = new List<string>
                    {
                        "sentence-transformers/all-MiniLM-L6-v2",
                        "distilbert-base-uncased-finetuned-sst-2-english",
                        "en_core_web_sm"
                    }
                }
            }
        };
    }
}

/// <summary>
/// Worker pool configuration.
/// </summary>
public sealed class PythonWorkerPoolOptions
{
    /// <summary>Gets or sets the pool name.</summary>
    public string Name { get; set; } = "cpu";

    /// <summary>Gets or sets the pool role.</summary>
    public PythonWorkerRole Role { get; set; } = PythonWorkerRole.Cpu;

    /// <summary>Gets or sets the number of workers in the pool.</summary>
    public int WorkerCount { get; set; } = 1;

    /// <summary>Gets or sets the maximum concurrent requests per worker.</summary>
    public int MaxConcurrentRequestsPerWorker { get; set; } = 4;

    /// <summary>Gets or sets the worker-specific model list.</summary>
    public List<string> Models { get; set; } = new();
}

/// <summary>
/// Custom function registration for safe Python invocation.
/// </summary>
public sealed class PythonCustomFunctionOptions
{
    /// <summary>Gets or sets the operation name.</summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>Gets or sets the module name.</summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>Gets or sets the function name.</summary>
    public string Function { get; set; } = string.Empty;

    /// <summary>Gets or sets the worker role hint.</summary>
    public PythonWorkerRole PreferredRole { get; set; } = PythonWorkerRole.Generic;

    /// <summary>Gets or sets the model name bound to the function.</summary>
    public string? Model { get; set; }
}

/// <summary>
/// Request payload exchanged with the Python worker.
/// </summary>
public sealed record PythonRequest(
    string RequestId,
    string Operation,
    string? Model,
    Dictionary<string, object?> Parameters,
    string? CorrelationId = null,
    string? WorkerRole = null,
    bool? Stream = null,
    int? ProtocolVersion = null);

/// <summary>
/// Response payload exchanged with the Python worker.
/// </summary>
public sealed record PythonResponse
{
    /// <summary>Gets or sets the request identifier.</summary>
    public string RequestId { get; init; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the request succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Gets or sets the result payload.</summary>
    public JsonElement? Result { get; init; }

    /// <summary>Gets or sets the worker error.</summary>
    public PythonErrorResponse? Error { get; init; }

    /// <summary>Gets or sets the worker identifier.</summary>
    public string? WorkerId { get; init; }

    /// <summary>Gets or sets the correlation identifier.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Gets or sets the elapsed duration in milliseconds.</summary>
    public long? DurationMs { get; init; }
}

/// <summary>
/// Structured error payload returned by the worker.
/// </summary>
public sealed record PythonErrorResponse(
    string Code,
    string Message,
    string? Details = null,
    string? StackTrace = null);

/// <summary>
/// Base exception for the Python AI runtime.
/// </summary>
public class PythonAIException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="PythonAIException"/> class.</summary>
    public PythonAIException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="PythonAIException"/> class.</summary>
    public PythonAIException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Raised when the worker exceeds its timeout.
/// </summary>
public sealed class PythonRequestTimeoutException : PythonAIException
{
    /// <summary>Initializes a new instance of the <see cref="PythonRequestTimeoutException"/> class.</summary>
    public PythonRequestTimeoutException(string message) : base(message) { }
}

/// <summary>
/// Raised when the worker process exits unexpectedly.
/// </summary>
public sealed class PythonWorkerCrashException : PythonAIException
{
    /// <summary>Initializes a new instance of the <see cref="PythonWorkerCrashException"/> class.</summary>
    public PythonWorkerCrashException(string message) : base(message) { }
}

/// <summary>
/// Raised when no healthy workers are available.
/// </summary>
public sealed class PythonWorkerUnavailableException : PythonAIException
{
    /// <summary>Initializes a new instance of the <see cref="PythonWorkerUnavailableException"/> class.</summary>
    public PythonWorkerUnavailableException(string message) : base(message) { }
}

/// <summary>
/// Raised when the worker sends malformed data.
/// </summary>
public sealed class PythonProtocolException : PythonAIException
{
    /// <summary>Initializes a new instance of the <see cref="PythonProtocolException"/> class.</summary>
    public PythonProtocolException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="PythonProtocolException"/> class.</summary>
    public PythonProtocolException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Snapshot of the runtime health.
/// </summary>
public sealed record PythonRuntimeSnapshot(
    int TotalWorkers,
    int HealthyWorkers,
    int BusyWorkers,
    int QueuedRequests,
    bool Started,
    string? LastError);

/// <summary>
/// Contract for the Python process manager.
/// </summary>
public interface IPythonProcessManager : IAsyncDisposable
{
    /// <summary>Starts the worker pool.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops the worker pool.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the current runtime snapshot.</summary>
    PythonRuntimeSnapshot GetSnapshot();

    /// <summary>Invokes a request and returns the raw response.</summary>
    Task<PythonResponse> InvokeAsync(PythonRequest request, CancellationToken cancellationToken = default);

    /// <summary>Invokes a request and deserializes the response result.</summary>
    Task<T> InvokeAsync<T>(PythonRequest request, CancellationToken cancellationToken = default);

    /// <summary>Runs an embeddings request.</summary>
    Task<float[]> GetEmbeddingAsync(string text, string model, CancellationToken cancellationToken = default);

    /// <summary>Runs a classification request.</summary>
    Task<PythonResponse> ClassifyAsync(string text, string model, CancellationToken cancellationToken = default);

    /// <summary>Runs a named entity extraction request.</summary>
    Task<IReadOnlyDictionary<string, List<string>>> ExtractEntitiesAsync(string text, string model, CancellationToken cancellationToken = default);

    /// <summary>Invokes a custom Python function registered in the worker.</summary>
    Task<T> InvokeCustomAsync<T>(string functionName, IDictionary<string, object?> arguments, string? model = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Contract for the worker transport.
/// </summary>
internal interface IPythonTransport : IAsyncDisposable
{
    /// <summary>Gets the process identifier.</summary>
    int ProcessId { get; }

    /// <summary>Gets a value indicating whether the underlying process is alive.</summary>
    bool IsAlive { get; }

    /// <summary>Starts the underlying worker.</summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>Sends a line-delimited JSON payload.</summary>
    Task SendLineAsync(string payload, CancellationToken cancellationToken);

    /// <summary>Reads the next line-delimited JSON payload.</summary>
    Task<string?> ReadLineAsync(CancellationToken cancellationToken);

    /// <summary>Requests a graceful shutdown.</summary>
    Task RequestShutdownAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Health snapshot exposed to ASP.NET health checks.
/// </summary>
public sealed class PythonHealthState
{
    /// <summary>Gets or sets the runtime snapshot.</summary>
    public PythonRuntimeSnapshot Snapshot { get; set; } = new(0, 0, 0, 0, false, null);

    /// <summary>Gets or sets the timestamp of the last healthy heartbeat.</summary>
    public DateTimeOffset? LastHealthyHeartbeat { get; set; }
}
