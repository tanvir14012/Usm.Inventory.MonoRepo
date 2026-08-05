namespace Shared.AI.Python;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Persistent Python worker manager with pool scheduling and restart support.
/// </summary>
public sealed class PythonProcessManager : IPythonProcessManager
{
    private static readonly Regex OperationNamePattern = new("^[a-zA-Z0-9._-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ModelNamePattern = new("^[a-zA-Z0-9._/:-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly PythonAIOptions _options;
    private readonly ILogger<PythonProcessManager> _logger;
    private readonly PythonWorkerPool _pool;
    private bool _started;

    /// <summary>
    /// Initializes a new instance of the <see cref="PythonProcessManager"/> class.
    /// </summary>
    public PythonProcessManager(IOptions<PythonAIOptions> options, ILogger<PythonProcessManager> logger, ILoggerFactory loggerFactory)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _pool = new PythonWorkerPool(_options.Pools.Count == 0 ? PythonAIOptions.CreateDefault().Pools : _options.Pools, _options, loggerFactory);
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started)
        {
            return;
        }

        _logger.LogInformation("Starting Python worker pool with {WorkerCount} workers.", _pool.Workers.Count);
        await _pool.StartAsync(cancellationToken).ConfigureAwait(false);
        _started = true;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_started)
        {
            return;
        }

        _logger.LogInformation("Stopping Python worker pool.");
        await _pool.StopAsync(cancellationToken).ConfigureAwait(false);
        _started = false;
    }

    /// <inheritdoc />
    public PythonRuntimeSnapshot GetSnapshot() => _pool.Snapshot();

    /// <inheritdoc />
    public async Task<PythonResponse> InvokeAsync(PythonRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var preferredRole = ResolvePreferredRole(request);
        var attempts = Math.Max(1, _options.MaxRequestRetries + 1);
        Exception? lastError = null;
        var started = Stopwatch.StartNew();
        PythonAIMetrics.RequestsStarted.Add(1, new KeyValuePair<string, object?>("operation", request.Operation));

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            var worker = _pool.SelectWorker(preferredRole);
            try
            {
                _logger.LogDebug("Dispatching {Operation} to {WorkerId} (attempt {Attempt}/{Attempts})", request.Operation, worker.WorkerId, attempt, attempts);
                var response = await worker.InvokeAsync(request, cancellationToken).ConfigureAwait(false);
                started.Stop();
                PythonAIMetrics.RequestsSucceeded.Add(1, new KeyValuePair<string, object?>("operation", request.Operation));
                PythonAIMetrics.RequestDurationMs.Record(started.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", request.Operation));
                return response;
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < attempts)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Worker {WorkerId} failed for operation {Operation}; retrying.", worker.WorkerId, request.Operation);
                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lastError = ex;
                break;
            }
        }

        started.Stop();
        PythonAIMetrics.RequestsFailed.Add(1, new KeyValuePair<string, object?>("operation", request.Operation));
        PythonAIMetrics.RequestDurationMs.Record(started.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", request.Operation));
        throw lastError is null
            ? new PythonAIException("The Python worker request failed without an exception.")
            : lastError;
    }

    /// <inheritdoc />
    public async Task<T> InvokeAsync<T>(PythonRequest request, CancellationToken cancellationToken = default)
    {
        var response = await InvokeAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.Result is null)
        {
            return default!;
        }

        return response.Result.Value.Deserialize<T>(PythonJson.ResponseOptions)
            ?? throw new PythonProtocolException($"Unable to deserialize the Python worker result as {typeof(T).Name}.");
    }

    /// <inheritdoc />
    public async Task<float[]> GetEmbeddingAsync(string text, string model, CancellationToken cancellationToken = default)
    {
        var response = await InvokeAsync(new PythonRequest(
            Guid.NewGuid().ToString("N"),
            PythonOperations.Embedding,
            model,
            new Dictionary<string, object?>
            {
                ["text"] = text
            }), cancellationToken).ConfigureAwait(false);

        return response.Result.HasValue
            ? response.Result.Value.Deserialize<float[]>(PythonJson.ResponseOptions) ?? Array.Empty<float>()
            : Array.Empty<float>();
    }

    /// <inheritdoc />
    public Task<PythonResponse> ClassifyAsync(string text, string model, CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(PythonOperations.Classification, model, new Dictionary<string, object?>
        {
            ["text"] = text
        });

        return InvokeAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, List<string>>> ExtractEntitiesAsync(string text, string model, CancellationToken cancellationToken = default)
    {
        var response = await InvokeAsync(BuildRequest(PythonOperations.Ner, model, new Dictionary<string, object?>
        {
            ["text"] = text
        }), cancellationToken).ConfigureAwait(false);

        if (!response.Result.HasValue)
        {
            return new ReadOnlyDictionary<string, List<string>>(new Dictionary<string, List<string>>());
        }

        return response.Result.Value.Deserialize<Dictionary<string, List<string>>>(PythonJson.ResponseOptions)
            ?? new Dictionary<string, List<string>>();
    }

    /// <inheritdoc />
    public async Task<T> InvokeCustomAsync<T>(string functionName, IDictionary<string, object?> arguments, string? model = null, CancellationToken cancellationToken = default)
    {
        var response = await InvokeAsync(new PythonRequest(
            Guid.NewGuid().ToString("N"),
            PythonOperations.Invoke,
            model,
            new Dictionary<string, object?>
            {
                ["function"] = functionName,
                ["arguments"] = arguments is Dictionary<string, object?> dict ? dict : arguments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            }), cancellationToken).ConfigureAwait(false);

        if (!response.Result.HasValue)
        {
            return default!;
        }

        return response.Result.Value.Deserialize<T>(PythonJson.ResponseOptions)
            ?? throw new PythonProtocolException($"Unable to deserialize custom function result as {typeof(T).Name}.");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private PythonRequest BuildRequest(string operation, string? model, Dictionary<string, object?> parameters, string? workerRole = null)
    {
        return new PythonRequest(
            Guid.NewGuid().ToString("N"),
            operation,
            model,
            parameters,
            WorkerRole: workerRole,
            ProtocolVersion: _options.ProtocolVersion);
    }

    private void ValidateRequest(PythonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Operation) || !OperationNamePattern.IsMatch(request.Operation))
        {
            throw new ArgumentException($"Invalid operation name '{request.Operation}'.", nameof(request));
        }

        if (_options.AllowedOperations.Count > 0 && !_options.AllowedOperations.Contains(request.Operation, StringComparer.OrdinalIgnoreCase))
        {
            throw new PythonAIException($"Operation '{request.Operation}' is not allowed by configuration.");
        }

        if (!string.IsNullOrWhiteSpace(request.Model) && !ModelNamePattern.IsMatch(request.Model))
        {
            throw new ArgumentException($"Invalid model name '{request.Model}'.", nameof(request));
        }

        if (request.Parameters.Any(pair => string.IsNullOrWhiteSpace(pair.Key)))
        {
            throw new ArgumentException("Request parameters contain an empty key.", nameof(request));
        }
    }

    private static PythonWorkerRole? ResolvePreferredRole(PythonRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.WorkerRole) && Enum.TryParse<PythonWorkerRole>(request.WorkerRole, ignoreCase: true, out var explicitRole))
        {
            return explicitRole;
        }

        return request.Operation.ToLowerInvariant() switch
        {
            PythonOperations.Ner => PythonWorkerRole.Nlp,
            PythonOperations.Summarization => PythonWorkerRole.Nlp,
            PythonOperations.Embedding => PythonWorkerRole.Cpu,
            PythonOperations.Embeddings => PythonWorkerRole.Cpu,
            PythonOperations.Sentiment => PythonWorkerRole.Nlp,
            PythonOperations.Translation => PythonWorkerRole.Cpu,
            PythonOperations.Classification => PythonWorkerRole.Cpu,
            PythonOperations.Invoke => PythonWorkerRole.Generic,
            _ => null
        };
    }

    private static bool IsTransient(Exception exception)
    {
        return exception is PythonWorkerCrashException
            or PythonWorkerUnavailableException
            or TimeoutException
            or PythonRequestTimeoutException
            or PythonProtocolException;
    }
}

/// <summary>
/// Convenience wrapper for transformer operations.
/// </summary>
public sealed class TransformersWrapper
{
    private readonly IPythonProcessManager _manager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransformersWrapper"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public TransformersWrapper(PersistentPythonBridge bridge)
        : this((IPythonProcessManager)bridge)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransformersWrapper"/> class.
    /// </summary>
    public TransformersWrapper(IPythonProcessManager manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>
    /// Classifies text using a transformers pipeline.
    /// </summary>
    public async Task<PythonResponse> ClassifyTextAsync(string text, string model = "distilbert-base-uncased-finetuned-sst-2-english", CancellationToken cancellationToken = default)
    {
        return await _manager.ClassifyAsync(text, model, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an embedding from a sentence-transformers model.
    /// </summary>
    public Task<float[]> GetEmbeddingAsync(string text, string model = "sentence-transformers/all-MiniLM-L6-v2", CancellationToken cancellationToken = default)
    {
        return _manager.GetEmbeddingAsync(text, model, cancellationToken);
    }

    /// <summary>
    /// Summarizes text with a transformers model.
    /// </summary>
    public async Task<string> SummarizeAsync(string text, string model = "facebook/bart-large-cnn", CancellationToken cancellationToken = default)
    {
        var response = await _manager.InvokeAsync(new PythonRequest(
            Guid.NewGuid().ToString("N"),
            PythonOperations.Summarization,
            model,
            new Dictionary<string, object?>
            {
                ["text"] = text
            }), cancellationToken).ConfigureAwait(false);

        if (!response.Result.HasValue)
        {
            return string.Empty;
        }

        var json = response.Result.Value;
        if (json.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            return json.GetString() ?? string.Empty;
        }

        if (json.ValueKind == System.Text.Json.JsonValueKind.Array && json.GetArrayLength() > 0)
        {
            var first = json[0];
            if (first.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (first.TryGetProperty("summary_text", out var summaryText))
                {
                    return summaryText.GetString() ?? string.Empty;
                }

                if (first.TryGetProperty("generated_text", out var generatedText))
                {
                    return generatedText.GetString() ?? string.Empty;
                }
            }
        }

        return json.GetRawText();
    }
}

/// <summary>
/// Convenience wrapper for spaCy operations.
/// </summary>
public sealed class spaCyWrapper
{
    private readonly IPythonProcessManager _manager;

    /// <summary>
    /// Initializes a new instance of the <see cref="spaCyWrapper"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public spaCyWrapper(PersistentPythonBridge bridge)
        : this((IPythonProcessManager)bridge)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="spaCyWrapper"/> class.
    /// </summary>
    public spaCyWrapper(IPythonProcessManager manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>
    /// Extracts named entities from text.
    /// </summary>
    public Task<IReadOnlyDictionary<string, List<string>>> ExtractEntitiesAsync(string text, string model = "en_core_web_sm", CancellationToken cancellationToken = default)
    {
        return _manager.ExtractEntitiesAsync(text, model, cancellationToken);
    }
}
