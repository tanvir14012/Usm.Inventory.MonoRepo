namespace Shared.AI.EngineClient;

using System.Diagnostics;
using System.Text;

/// <summary>
/// Thread-safe gRPC client for the AI engine.
/// </summary>
public sealed class AiEngineClient : IAIEngineClient
{
    private readonly AIEngineService.AIEngineServiceClient _grpcClient;
    private readonly AiEngineClientOptions _options;
    private readonly ILogger<AiEngineClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiEngineClient"/> class.
    /// </summary>
    public AiEngineClient(
        AIEngineService.AIEngineServiceClient grpcClient,
        IOptions<AiEngineClientOptions> options,
        ILogger<AiEngineClient> logger)
    {
        _grpcClient = grpcClient;
        _options = options.Value;
        _logger = logger;
        _jsonOptions = AiEngineClientOptions.CreateSerializerOptions();
    }

    /// <inheritdoc />
    public async Task<TResult?> ExecuteAsync<TResult>(string taskType, object payload, CancellationToken ct = default)
    {
        var request = BuildRequest(taskType, payload);
        var callOptions = CreateCallOptions(ct);
        var stopwatch = Stopwatch.StartNew();

        var response = await _grpcClient.ExecuteTaskAsync(request, callOptions).ConfigureAwait(false);
        stopwatch.Stop();

        if (!string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            throw new AiEngineRemoteException(BuildErrorMessage(response.ResultJson, response.Metadata), response.Metadata);
        }

        if (string.IsNullOrWhiteSpace(response.ResultJson))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<TResult>(response.ResultJson, _jsonOptions);
        }
        catch (Exception ex)
        {
            throw new AiEngineProtocolException($"Failed to deserialize '{taskType}' result as {typeof(TResult).Name}.", ex);
        }
        finally
        {
            _logger.LogDebug("Executed task {TaskType} in {Elapsed}ms", taskType, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TStreamItem> StreamAsync<TStreamItem>(
        string taskType,
        object payload,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = BuildRequest(taskType, payload);
        var callOptions = CreateCallOptions(ct);
        using var call = _grpcClient.StreamTask(request, callOptions);
        await foreach (var message in call.ResponseStream.ReadAllAsync(ct).ConfigureAwait(false))
        {
            if (string.Equals(message.EventType, "error", StringComparison.OrdinalIgnoreCase))
            {
                throw new AiEngineRemoteException(BuildErrorMessage(message.DataJson, null));
            }

            if (string.Equals(message.EventType, "complete", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(message.DataJson))
                {
                    yield return DeserializeStreamItem<TStreamItem>(message.DataJson, taskType);
                }

                yield break;
            }

            if (string.IsNullOrWhiteSpace(message.DataJson))
            {
                continue;
            }

            yield return DeserializeStreamItem<TStreamItem>(message.DataJson, taskType);
        }
    }

    private TaskRequest BuildRequest(string taskType, object payload)
    {
        if (string.IsNullOrWhiteSpace(taskType))
        {
            throw new ArgumentException("Task type is required.", nameof(taskType));
        }

        var request = new TaskRequest
        {
            TaskType = taskType,
            PayloadJson = JsonSerializer.Serialize(payload, _jsonOptions)
        };

        foreach (var pair in _options.DefaultMetadata)
        {
            request.Metadata[pair.Key] = pair.Value;
        }

        if (Activity.Current is not null)
        {
            request.Metadata["traceparent"] = Activity.Current.Id ?? string.Empty;
        }

        return request;
    }

    private CallOptions CreateCallOptions(CancellationToken ct)
    {
        return new CallOptions(deadline: DateTime.UtcNow.Add(_options.Timeout), cancellationToken: ct);
    }

    private TStreamItem DeserializeStreamItem<TStreamItem>(string dataJson, string taskType)
    {
        try
        {
            if (typeof(TStreamItem) == typeof(string))
            {
                var value = JsonSerializer.Deserialize<string>(dataJson, _jsonOptions);
                return (TStreamItem)(object)(value ?? string.Empty);
            }

            return JsonSerializer.Deserialize<TStreamItem>(dataJson, _jsonOptions)
                ?? throw new AiEngineProtocolException($"Stream item for '{taskType}' deserialized to null.");
        }
        catch (Exception ex)
        {
            throw new AiEngineProtocolException($"Failed to deserialize stream item for '{taskType}' as {typeof(TStreamItem).Name}.", ex);
        }
    }

    private static string BuildErrorMessage(string? payloadJson, IReadOnlyDictionary<string, string>? metadata)
    {
        if (!string.IsNullOrWhiteSpace(payloadJson))
        {
            return payloadJson;
        }

        if (metadata is not null && metadata.TryGetValue("error", out var error))
        {
            return error;
        }

        return "The AI engine returned an error response.";
    }
}
