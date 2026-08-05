namespace Shared.AI.Python;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.AI.Abstractions;

public sealed class PersistentPythonBridge : IAsyncDisposable, IPythonProcessManager
{
    private readonly PythonAIOptions _options;
    private readonly ILogger<PersistentPythonBridge> _logger;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly PythonWorkerRouter _router;
    private bool _started;

    public PersistentPythonBridge(IOptions<PythonAIOptions> options, ILogger<PersistentPythonBridge> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _router = new PythonWorkerRouter(_options, logger);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_started)
            {
                return;
            }

            await _router.StartAsync(cancellationToken).ConfigureAwait(false);
            _started = true;
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken = default) => InitializeAsync(cancellationToken);

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_started)
            {
                return;
            }

            await _router.StopAsync(cancellationToken).ConfigureAwait(false);
            _started = false;
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public PythonRuntimeSnapshot GetSnapshot() => _router.Snapshot(_started);

    public async Task<PythonResponse> InvokeAsync(PythonRequest request, CancellationToken cancellationToken = default)
    {
        if (!_started)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        return await _router.InvokeAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> InvokeAsync<T>(PythonRequest request, CancellationToken cancellationToken = default)
    {
        var response = await InvokeAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.Result.HasValue)
        {
            return default!;
        }

        return response.Result.Value.Deserialize<T>(PythonJson.ResponseOptions)!;
    }

    public Task<float[]> GetEmbeddingAsync(string text, string model, CancellationToken cancellationToken = default)
        => InvokeAsync<float[]>(PythonRequestFactory.Embedding(text, model), cancellationToken);

    public Task<PythonResponse> ClassifyAsync(string text, string model, CancellationToken cancellationToken = default)
        => InvokeAsync(PythonRequestFactory.Classification(text, model), cancellationToken);

    public async Task<IReadOnlyDictionary<string, List<string>>> ExtractEntitiesAsync(string text, string model, CancellationToken cancellationToken = default)
    {
        var result = await InvokeAsync<Dictionary<string, List<string>>>(PythonRequestFactory.Ner(text, model), cancellationToken).ConfigureAwait(false);
        return result;
    }

    public Task<T> InvokeCustomAsync<T>(string functionName, IDictionary<string, object?> arguments, string? model = null, CancellationToken cancellationToken = default)
        => InvokeAsync<T>(PythonRequestFactory.Custom(functionName, arguments, model), cancellationToken);

    public async ValueTask DisposeAsync() => await StopAsync(CancellationToken.None).ConfigureAwait(false);
}

internal static class PythonRequestFactory
{
    public static PythonRequest Embedding(string text, string model) =>
        new(Guid.NewGuid().ToString("N"), PythonOperations.Embedding, model, new Dictionary<string, object?> { ["text"] = text });

    public static PythonRequest Classification(string text, string model) =>
        new(Guid.NewGuid().ToString("N"), PythonOperations.Classification, model, new Dictionary<string, object?> { ["text"] = text });

    public static PythonRequest Ner(string text, string model) =>
        new(Guid.NewGuid().ToString("N"), PythonOperations.Ner, model, new Dictionary<string, object?> { ["text"] = text });

    public static PythonRequest Custom(string functionName, IDictionary<string, object?> arguments, string? model) =>
        new(Guid.NewGuid().ToString("N"), PythonOperations.Invoke, model, new Dictionary<string, object?> { ["function"] = functionName, ["arguments"] = arguments.ToDictionary(k => k.Key, v => v.Value) });
}

internal sealed class PythonWorkerRouter
{
    private readonly PythonAIOptions _options;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, PythonWorkerSession> _sessions = new();

    public PythonWorkerRouter(PythonAIOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var pools = _options.Pools.Count == 0 ? PythonAIOptions.CreateDefault().Pools : _options.Pools;
        foreach (var pool in pools)
        {
            var session = new PythonWorkerSession(pool, _options, _logger);
            await session.StartAsync(cancellationToken).ConfigureAwait(false);
            _sessions[pool.Name] = session;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var session in _sessions.Values)
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }

        _sessions.Clear();
    }

    public PythonRuntimeSnapshot Snapshot(bool started)
    {
        var sessions = _sessions.Values.ToList();
        return new PythonRuntimeSnapshot(
            sessions.Count,
            sessions.Count(s => s.IsHealthy),
            sessions.Count(s => s.IsBusy),
            sessions.Sum(s => s.QueuedRequests),
            started,
            sessions.Select(s => s.LastError).FirstOrDefault(e => e is not null));
    }

    public Task<PythonResponse> InvokeAsync(PythonRequest request, CancellationToken cancellationToken)
    {
        var session = Resolve(request);
        return session.InvokeAsync(request, cancellationToken);
    }

    private PythonWorkerSession Resolve(PythonRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.WorkerRole) && _sessions.TryGetValue(request.WorkerRole, out var session))
        {
            return session;
        }

        return _sessions.Values.OrderBy(s => s.BusyCount).FirstOrDefault()
            ?? throw new PythonWorkerUnavailableException("No Python workers are available.");
    }
}

internal sealed class PythonWorkerSession : IAsyncDisposable
{
    private readonly PythonAIOptions _options;
    private readonly PythonWorkerPoolOptions _definition;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _stdinLock = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<PythonResponse>> _pending = new();
    private Process? _process;
    private Task? _stdoutLoop;

    public PythonWorkerSession(PythonWorkerPoolOptions definition, PythonAIOptions options, ILogger logger)
    {
        _definition = definition;
        _options = options;
        _logger = logger;
    }

    public bool IsHealthy { get; private set; }
    public bool IsBusy => BusyCount > 0;
    public int BusyCount => _pending.Count;
    public int QueuedRequests => _pending.Count;
    public string? LastError { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_process is not null)
        {
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = _options.PythonExecutablePath ?? "python",
            Arguments = "-u worker.py",
            WorkingDirectory = _options.WorkerRootPath ?? AppContext.BaseDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _process = Process.Start(psi) ?? throw new PythonAIException("Failed to start Python worker.");
        _stdoutLoop = Task.Run(ReadStdoutLoopAsync, cancellationToken);
        var heartbeat = await InvokeAsync(new PythonRequest(Guid.NewGuid().ToString("N"), PythonOperations.Heartbeat, null, new Dictionary<string, object?> { ["pool"] = _definition.Name }), cancellationToken).ConfigureAwait(false);
        IsHealthy = heartbeat.Success;
    }

    public async Task<PythonResponse> InvokeAsync(PythonRequest request, CancellationToken cancellationToken)
    {
        if (_process is null)
        {
            throw new PythonWorkerUnavailableException("Worker is not started.");
        }

        var tcs = new TaskCompletionSource<PythonResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[request.RequestId] = tcs;

        var payload = JsonSerializer.Serialize(request, PythonJson.RequestOptions);
        await _stdinLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _process.StandardInput.WriteLineAsync(payload.AsMemory(), cancellationToken).ConfigureAwait(false);
            await _process.StandardInput.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            _stdinLock.Release();
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.RequestTimeout);
        using var registration = timeout.Token.Register(() => tcs.TrySetException(new PythonRequestTimeoutException($"Python request {request.RequestId} timed out.")));

        var response = await tcs.Task.ConfigureAwait(false);
        IsHealthy = response.Success;
        LastError = response.Error?.Message;
        return response;
    }

    private async Task ReadStdoutLoopAsync()
    {
        if (_process is null)
        {
            return;
        }

        while (!_process.HasExited)
        {
            var line = await _process.StandardOutput.ReadLineAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var response = JsonSerializer.Deserialize<PythonResponse>(line, PythonJson.ResponseOptions);
            if (response is null)
            {
                continue;
            }

            if (_pending.TryRemove(response.RequestId, out var tcs))
            {
                tcs.TrySetResult(response);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            await _process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new PythonRequest(Guid.NewGuid().ToString("N"), PythonOperations.Shutdown, null, new Dictionary<string, object?> { ["pool"] = _definition.Name }), PythonJson.RequestOptions)).ConfigureAwait(false);
        }
        catch
        {
        }

        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
        }

        _process.Dispose();
    }
}
