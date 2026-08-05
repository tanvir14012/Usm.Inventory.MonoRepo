namespace Shared.AI.EngineClient;

/// <summary>
/// Client abstraction for the AI engine service.
/// </summary>
public interface IAIEngineClient
{
    /// <summary>
    /// Executes a single-turn AI task.
    /// </summary>
    Task<TResult?> ExecuteAsync<TResult>(string taskType, object payload, CancellationToken ct = default);

    /// <summary>
    /// Streams AI task output.
    /// </summary>
    IAsyncEnumerable<TStreamItem> StreamAsync<TStreamItem>(string taskType, object payload, CancellationToken ct = default);
}

