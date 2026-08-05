namespace Shared.AI.Python;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.AI.Abstractions;

/// <summary>
/// Public bridge over the persistent Python worker runtime.
/// </summary>
public sealed class PersistentPythonBridge : IPythonProcessManager
{
    private readonly PythonProcessManager _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistentPythonBridge"/> class.
    /// </summary>
    public PersistentPythonBridge(IOptions<PythonAIOptions> options, ILogger<PersistentPythonBridge> logger, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _inner = new PythonProcessManager(options, loggerFactory.CreateLogger<PythonProcessManager>(), loggerFactory);
        logger.LogDebug("Persistent Python bridge initialized.");
    }

    /// <summary>
    /// Pre-warms the persistent worker runtime.
    /// </summary>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return _inner.StartAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default) => InitializeAsync(cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default) => _inner.StopAsync(cancellationToken);

    /// <inheritdoc />
    public PythonRuntimeSnapshot GetSnapshot() => _inner.GetSnapshot();

    /// <inheritdoc />
    public Task<PythonResponse> InvokeAsync(PythonRequest request, CancellationToken cancellationToken = default) => _inner.InvokeAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task<T> InvokeAsync<T>(PythonRequest request, CancellationToken cancellationToken = default) => _inner.InvokeAsync<T>(request, cancellationToken);

    /// <inheritdoc />
    public Task<float[]> GetEmbeddingAsync(string text, string model, CancellationToken cancellationToken = default) => _inner.GetEmbeddingAsync(text, model, cancellationToken);

    /// <inheritdoc />
    public Task<PythonResponse> ClassifyAsync(string text, string model, CancellationToken cancellationToken = default) => _inner.ClassifyAsync(text, model, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, List<string>>> ExtractEntitiesAsync(string text, string model, CancellationToken cancellationToken = default) => _inner.ExtractEntitiesAsync(text, model, cancellationToken);

    /// <inheritdoc />
    public Task<T> InvokeCustomAsync<T>(string functionName, IDictionary<string, object?> arguments, string? model = null, CancellationToken cancellationToken = default) => _inner.InvokeCustomAsync<T>(functionName, arguments, model, cancellationToken);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _inner.DisposeAsync();
}
