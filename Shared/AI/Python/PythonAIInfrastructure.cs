namespace Shared.AI.Python;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal static class PythonJson
{
    internal static readonly JsonSerializerOptions RequestOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    internal static readonly JsonSerializerOptions ResponseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}

internal static class PythonAIMetrics
{
    internal static readonly Meter Meter = new("Shared.AI.Python", "1.0.0");
    internal static readonly Counter<long> RequestsStarted = Meter.CreateCounter<long>("python_ai.requests.started");
    internal static readonly Counter<long> RequestsSucceeded = Meter.CreateCounter<long>("python_ai.requests.succeeded");
    internal static readonly Counter<long> RequestsFailed = Meter.CreateCounter<long>("python_ai.requests.failed");
    internal static readonly Histogram<double> RequestDurationMs = Meter.CreateHistogram<double>("python_ai.request.duration.ms");
}

internal static class PythonExecutableFinder
{
    internal static async Task<string> ResolveAsync(PythonAIOptions options, CancellationToken cancellationToken)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(options.PythonExecutablePath))
        {
            candidates.Add(options.PythonExecutablePath!);
        }

        if (!string.IsNullOrWhiteSpace(options.VirtualEnvironmentPath))
        {
            var venv = options.VirtualEnvironmentPath!;
            candidates.Add(OperatingSystem.IsWindows()
                ? Path.Combine(venv, "Scripts", "python.exe")
                : Path.Combine(venv, "bin", "python"));
            candidates.Add(OperatingSystem.IsWindows()
                ? Path.Combine(venv, "Scripts", "python3.exe")
                : Path.Combine(venv, "bin", "python3"));
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PYTHON_EXECUTABLE")))
        {
            candidates.Add(Environment.GetEnvironmentVariable("PYTHON_EXECUTABLE")!);
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PYTHON")))
        {
            candidates.Add(Environment.GetEnvironmentVariable("PYTHON")!);
        }

        candidates.AddRange(OperatingSystem.IsWindows()
            ? new[] { "python.exe", "python3.exe", "python" }
            : new[] { "python3", "python" });

        foreach (var candidate in candidates.Where(static c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (await CanExecuteAsync(candidate, cancellationToken).ConfigureAwait(false))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Unable to locate a usable Python executable. Set PythonExecutablePath or install Python 3.10+.");
    }

    private static async Task<bool> CanExecuteAsync(string candidate, CancellationToken cancellationToken)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = candidate,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return process.ExitCode == 0 || process.ExitCode == 1;
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class StdioJsonTransport : IPythonTransport
{
    private readonly string _pythonExecutable;
    private readonly string _workerModule;
    private readonly string _bootstrapConfig;
    private readonly string _workerId;
    private readonly string? _workerRootPath;
    private readonly ILogger _logger;
    private readonly object _writeGate = new();
    private Process? _process;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;
    private StreamReader? _stderr;
    private bool _disposed;

    internal StdioJsonTransport(
        string pythonExecutable,
        string workerModule,
        string bootstrapConfig,
        string workerId,
        string? workerRootPath,
        ILogger logger)
    {
        _pythonExecutable = pythonExecutable;
        _workerModule = workerModule;
        _bootstrapConfig = bootstrapConfig;
        _workerId = workerId;
        _workerRootPath = workerRootPath;
        _logger = logger;
    }

    public int ProcessId => _process?.Id ?? 0;

    public bool IsAlive => _process is { HasExited: false };

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(StdioJsonTransport));
        }

        if (_process is not null)
        {
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = _pythonExecutable,
            Arguments = $"-u -m {_workerModule}",
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        psi.Environment["USM_SHARED_AI_CONFIG"] = _bootstrapConfig;
        psi.Environment["USM_SHARED_AI_WORKER_ID"] = _workerId;

        if (!string.IsNullOrWhiteSpace(_workerRootPath))
        {
            var resolvedWorkerRootPath = Path.IsPathRooted(_workerRootPath)
                ? _workerRootPath
                : Path.GetFullPath(_workerRootPath, AppContext.BaseDirectory);
            var currentPythonPath = psi.Environment.ContainsKey("PYTHONPATH")
                ? psi.Environment["PYTHONPATH"]
                : string.Empty;
            psi.Environment["PYTHONPATH"] = string.IsNullOrWhiteSpace(currentPythonPath)
                ? resolvedWorkerRootPath
                : string.Join(Path.PathSeparator, new[] { resolvedWorkerRootPath, currentPythonPath });
        }

        _process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start the Python worker process.");
        _stdin = _process.StandardInput;
        _stdout = _process.StandardOutput;
        _stderr = _process.StandardError;

        _ = Task.Run(() => PumpErrorAsync(_stderr, cancellationToken), cancellationToken);
        _logger.LogInformation("Started Python worker process {ProcessId} for {WorkerId}", _process.Id, _workerId);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task SendLineAsync(string payload, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_stdin is null)
        {
            throw new InvalidOperationException("Worker transport is not started.");
        }

        lock (_writeGate)
        {
            _stdin.WriteLine(payload);
        }

        await _stdin.FlushAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_stdout is null)
        {
            throw new InvalidOperationException("Worker transport is not started.");
        }

        return await _stdout.ReadLineAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RequestShutdownAsync(CancellationToken cancellationToken)
    {
        if (_stdin is null || _process is null || _process.HasExited)
        {
            return;
        }

        var shutdownRequest = new PythonRequest(
            Guid.NewGuid().ToString("N"),
            PythonOperations.Shutdown,
            null,
            new Dictionary<string, object?>(),
            ProtocolVersion: 1);

        await SendLineAsync(JsonSerializer.Serialize(shutdownRequest, PythonJson.RequestOptions), cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            if (_process is not null && !_process.HasExited)
            {
                try
                {
                    _process.Kill(entireProcessTree: true);
                }
                catch
                {
                }
            }
        }
        finally
        {
            _stdin?.Dispose();
            _stdout?.Dispose();
            _stderr?.Dispose();
            _process?.Dispose();
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    private async Task PumpErrorAsync(StreamReader? reader, CancellationToken cancellationToken)
    {
        if (reader is null)
        {
            return;
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                _logger.LogError("Python worker stderr [{WorkerId}]: {Line}", _workerId, line);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "stderr pump for worker {WorkerId} ended", _workerId);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(StdioJsonTransport));
        }
    }
}

internal sealed class PythonWorkerProcess : IAsyncDisposable
{
    private readonly PythonWorkerPoolOptions _pool;
    private readonly PythonAIOptions _options;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _capacity;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<PythonResponse>> _pending = new();
    private readonly CancellationTokenSource _lifetimeCts = new();
    private readonly object _stateGate = new();
    private readonly TaskCompletionSource<bool> _readySignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task? _supervisorTask;
    private IPythonTransport? _transport;
    private bool _started;
    private bool _stopping;
    private int _generation;
    private int _inFlight;
    private long _lastHeartbeatTicks;
    private string? _lastError;
    private DateTimeOffset _lastStartedAt;

    internal PythonWorkerProcess(PythonWorkerPoolOptions pool, PythonAIOptions options, ILogger logger)
    {
        _pool = pool;
        _options = options;
        _logger = logger;
        _capacity = new SemaphoreSlim(Math.Max(1, pool.MaxConcurrentRequestsPerWorker), Math.Max(1, pool.MaxConcurrentRequestsPerWorker));
    }

    public string WorkerId => $"{_pool.Name}-{_generation}";

    public PythonWorkerRole Role => _pool.Role;

    public int InFlight => Volatile.Read(ref _inFlight);

    public bool IsHealthy => _started && _transport is { IsAlive: true };

    public string? LastError => Volatile.Read(ref _lastError);

    public DateTimeOffset LastStartedAt => _lastStartedAt;

    public int ProcessId => _transport?.ProcessId ?? 0;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_started)
        {
            return;
        }

        lock (_stateGate)
        {
            if (_started)
            {
                return;
            }

            _supervisorTask ??= RunSupervisorAsync();
            _started = true;
        }

        await _readySignal.Task.WaitAsync(_options.StartupTimeout, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PythonResponse> InvokeAsync(PythonRequest request, CancellationToken cancellationToken)
    {
        if (!_started)
        {
            throw new InvalidOperationException("Worker has not been started.");
        }

        await _capacity.WaitAsync(cancellationToken).ConfigureAwait(false);
        Interlocked.Increment(ref _inFlight);

        try
        {
            var transport = await WaitForTransportAsync(cancellationToken).ConfigureAwait(false);
            var effectiveRequest = request with
            {
                WorkerRole = _pool.Role.ToString(),
                ProtocolVersion = request.ProtocolVersion ?? _options.ProtocolVersion
            };

            var payload = JsonSerializer.Serialize(effectiveRequest, PythonJson.RequestOptions);
            var pending = new TaskCompletionSource<PythonResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_pending.TryAdd(request.RequestId, pending))
            {
                throw new PythonProtocolException($"Duplicate request id '{request.RequestId}'.");
            }

            try
            {
                using var registration = cancellationToken.Register(() =>
                {
                    if (_pending.TryRemove(request.RequestId, out var source))
                    {
                        source.TrySetCanceled(cancellationToken);
                    }
                });

                await transport.SendLineAsync(payload, cancellationToken).ConfigureAwait(false);
                var timeout = request.Parameters.TryGetValue("__timeoutMs", out var timeoutValue) && timeoutValue is int timeoutMs && timeoutMs > 0
                    ? TimeSpan.FromMilliseconds(timeoutMs)
                    : _options.RequestTimeout;

                var response = await pending.Task.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
                if (!response.Success)
                {
                    throw BuildWorkerException(response);
                }

                return response;
            }
            finally
            {
                _pending.TryRemove(request.RequestId, out _);
            }
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
            _capacity.Release();
        }
    }

    public PythonRuntimeSnapshot Snapshot()
    {
        var transport = _transport;
        var healthy = transport is { IsAlive: true } ? 1 : 0;
        var busy = InFlight > 0 ? 1 : 0;
        return new PythonRuntimeSnapshot(1, healthy, busy, Math.Max(0, _capacity.CurrentCount == 0 ? InFlight : 0), _started, LastError);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stopping = true;
        _lifetimeCts.Cancel();

        if (_transport is not null)
        {
            try
            {
                await _transport.RequestShutdownAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Graceful shutdown request failed for worker {WorkerId}", WorkerId);
            }

            await _transport.DisposeAsync().ConfigureAwait(false);
        }

        if (_supervisorTask is not null)
        {
            var completed = await Task.WhenAny(_supervisorTask, Task.Delay(TimeSpan.FromSeconds(10), cancellationToken)).ConfigureAwait(false);
            if (completed != _supervisorTask)
            {
                _logger.LogWarning("Python worker {WorkerId} did not stop cleanly within the timeout.", WorkerId);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        _capacity.Dispose();
        _lifetimeCts.Dispose();
    }

    private async Task RunSupervisorAsync()
    {
        var failures = 0;

        while (!_lifetimeCts.IsCancellationRequested)
        {
            IPythonTransport? transport = null;
            try
            {
                var pythonExecutable = await PythonExecutableFinder.ResolveAsync(_options, _lifetimeCts.Token).ConfigureAwait(false);
                transport = new StdioJsonTransport(
                    pythonExecutable,
                    _options.WorkerModule,
                    BuildBootstrapConfig(),
                    WorkerId,
                    _options.WorkerRootPath,
                    _logger);

                await transport.StartAsync(_lifetimeCts.Token).ConfigureAwait(false);
                lock (_stateGate)
                {
                    _transport = transport;
                    _generation++;
                    _lastStartedAt = DateTimeOffset.UtcNow;
                    _lastError = null;
                }

                await PerformStartupHandshakeAsync(transport, _lifetimeCts.Token).ConfigureAwait(false);
                _readySignal.TrySetResult(true);
                failures = 0;

                await PumpResponsesAsync(transport, _lifetimeCts.Token).ConfigureAwait(false);

                if (!_stopping && !transport.IsAlive)
                {
                    throw new PythonWorkerCrashException($"Python worker {WorkerId} exited unexpectedly.");
                }
            }
            catch (OperationCanceledException) when (_lifetimeCts.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                FailPending(ex);
                if (!_readySignal.Task.IsCompleted)
                {
                    _readySignal.TrySetException(ex);
                }

                if (_lifetimeCts.IsCancellationRequested || _stopping)
                {
                    break;
                }

                failures++;
                _logger.LogWarning(ex, "Python worker {WorkerId} failed (attempt {Attempt})", WorkerId, failures);
                await Task.Delay(TimeSpan.FromMilliseconds(_options.RestartDelay.TotalMilliseconds * Math.Min(failures, 5)), _lifetimeCts.Token).ConfigureAwait(false);
            }
            finally
            {
                if (transport is not null)
                {
                    if (ReferenceEquals(_transport, transport))
                    {
                        _transport = null;
                    }

                    await transport.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }

    private async Task PerformStartupHandshakeAsync(IPythonTransport transport, CancellationToken cancellationToken)
    {
        var request = new PythonRequest(
            Guid.NewGuid().ToString("N"),
            PythonOperations.Heartbeat,
            null,
            new Dictionary<string, object?>(),
            ProtocolVersion: _options.ProtocolVersion);

        var payload = JsonSerializer.Serialize(request, PythonJson.RequestOptions);
        await transport.SendLineAsync(payload, cancellationToken).ConfigureAwait(false);

        var responseJson = await transport.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            throw new PythonWorkerCrashException($"Python worker {WorkerId} did not respond during startup.");
        }

        var response = JsonSerializer.Deserialize<PythonResponse>(responseJson, PythonJson.ResponseOptions)
            ?? throw new PythonProtocolException("Invalid startup response.");

        if (!response.Success)
        {
            throw BuildWorkerException(response);
        }

        Volatile.Write(ref _lastHeartbeatTicks, DateTime.UtcNow.Ticks);
    }

    private async Task PumpResponsesAsync(IPythonTransport transport, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && transport.IsAlive)
        {
            var line = await transport.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                break;
            }

            PythonResponse? response;
            try
            {
                response = JsonSerializer.Deserialize<PythonResponse>(line, PythonJson.ResponseOptions);
            }
            catch (Exception ex)
            {
                throw new PythonProtocolException("Failed to deserialize the Python worker response.", ex);
            }

            if (response is null)
            {
                throw new PythonProtocolException("Python worker returned an empty response.");
            }

            if (response.RequestId == string.Empty)
            {
                continue;
            }

            if (response.OperationIsHeartbeat())
            {
                Volatile.Write(ref _lastHeartbeatTicks, DateTime.UtcNow.Ticks);
            }

            if (_pending.TryRemove(response.RequestId, out var pending))
            {
                pending.TrySetResult(response);
            }
        }
    }

    private async Task<IPythonTransport> WaitForTransportAsync(CancellationToken cancellationToken)
    {
        var timeout = _options.StartupTimeout;
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            var transport = _transport;
            if (transport is not null && transport.IsAlive)
            {
                return transport;
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        throw new PythonWorkerUnavailableException($"Worker {WorkerId} is not available.");
    }

    private void FailPending(Exception ex)
    {
        foreach (var pair in _pending)
        {
            if (_pending.TryRemove(pair.Key, out var pending))
            {
                pending.TrySetException(ex);
            }
        }
    }

    private static Exception BuildWorkerException(PythonResponse response)
    {
        var error = response.Error;
        if (error is null)
        {
            return new PythonAIException("The Python worker returned an unknown error.");
        }

        return new PythonAIException($"{error.Code}: {error.Message}");
    }

    private string BuildBootstrapConfig()
    {
        var bootstrap = new PythonWorkerBootstrapConfig
        {
            WorkerId = WorkerId,
            Role = _pool.Role.ToString(),
            PoolName = _pool.Name,
            ProtocolVersion = _options.ProtocolVersion,
            MinimumPythonVersion = _options.MinimumPythonVersion.ToString(),
            Models = _pool.Models.Select(m => new PythonWorkerModelBootstrapConfig
            {
                Name = m,
                Role = _pool.Role.ToString()
            }).ToList(),
            CustomFunctions = _options.CustomFunctions
                .Where(f => string.Equals(f.PreferredRole.ToString(), _pool.Role.ToString(), StringComparison.OrdinalIgnoreCase) || f.PreferredRole == PythonWorkerRole.Generic)
                .Select(f => new PythonCustomFunctionBootstrapConfig
                {
                    Operation = f.Operation,
                    Module = f.Module,
                    Function = f.Function,
                    Model = f.Model
                }).ToList(),
            WarmupModels = _options.WarmupModels
        };

        var json = JsonSerializer.Serialize(bootstrap, PythonJson.RequestOptions);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
}

internal sealed class PythonWorkerPool
{
    private readonly List<PythonWorkerProcess> _workers;
    private readonly PythonAIOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private int _roundRobinCursor;

    internal PythonWorkerPool(IEnumerable<PythonWorkerPoolOptions> pools, PythonAIOptions options, ILoggerFactory loggerFactory)
    {
        _options = options;
        _loggerFactory = loggerFactory;
        _workers = pools.SelectMany(CreateWorkers).ToList();
    }

    public IReadOnlyList<PythonWorkerProcess> Workers => _workers;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_workers.Select(worker => worker.StartAsync(cancellationToken))).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_workers.Select(worker => worker.StopAsync(cancellationToken))).ConfigureAwait(false);
    }

    public PythonRuntimeSnapshot Snapshot()
    {
        var snapshots = _workers.Select(worker => worker.Snapshot()).ToArray();
        return new PythonRuntimeSnapshot(
            snapshots.Length,
            snapshots.Count(s => s.HealthyWorkers > 0),
            _workers.Count(w => w.InFlight > 0),
            0,
            snapshots.Any(s => s.Started),
            snapshots.Select(s => s.LastError).FirstOrDefault(err => !string.IsNullOrWhiteSpace(err)));
    }

    public PythonWorkerProcess SelectWorker(PythonWorkerRole? preferredRole = null)
    {
        var healthy = _workers.Where(worker => worker.IsHealthy).ToArray();
        var matching = preferredRole.HasValue && preferredRole.Value != PythonWorkerRole.Generic
            ? healthy.Where(worker => worker.Role == preferredRole.Value || worker.Role == PythonWorkerRole.Generic).ToArray()
            : healthy;
        if (matching.Length == 0)
        {
            throw new PythonWorkerUnavailableException("No Python workers are configured.");
        }

        if (_options.Scheduling == PythonWorkerScheduling.RoundRobin)
        {
            var cursor = Interlocked.Increment(ref _roundRobinCursor);
            return matching[cursor % matching.Length];
        }

        return matching
            .OrderBy(worker => worker.InFlight)
            .ThenBy(worker => worker.ProcessId == 0 ? int.MaxValue : worker.ProcessId)
            .First();
    }

    private IEnumerable<PythonWorkerProcess> CreateWorkers(PythonWorkerPoolOptions pool)
    {
        for (var index = 0; index < Math.Max(1, pool.WorkerCount); index++)
        {
            yield return new PythonWorkerProcess(pool, _options, _loggerFactory.CreateLogger($"PythonWorker[{pool.Name}-{index}]"));
        }
    }
}

internal sealed record PythonWorkerBootstrapConfig
{
    public string WorkerId { get; init; } = string.Empty;
    public string PoolName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public int ProtocolVersion { get; init; }
    public string MinimumPythonVersion { get; init; } = string.Empty;
    public bool WarmupModels { get; init; }
    public List<PythonWorkerModelBootstrapConfig> Models { get; init; } = new();
    public List<PythonCustomFunctionBootstrapConfig> CustomFunctions { get; init; } = new();
}

internal sealed record PythonWorkerModelBootstrapConfig
{
    public string Name { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

internal sealed record PythonCustomFunctionBootstrapConfig
{
    public string Operation { get; init; } = string.Empty;
    public string Module { get; init; } = string.Empty;
    public string Function { get; init; } = string.Empty;
    public string? Model { get; init; }
}

internal static class PythonResponseExtensions
{
    internal static bool OperationIsHeartbeat(this PythonResponse response)
    {
        if (response.Result is null)
        {
            return false;
        }

        return response.Result.Value.ValueKind == JsonValueKind.Object
            && response.Result.Value.TryGetProperty("operation", out var op)
            && op.GetString() == PythonOperations.Heartbeat;
    }
}
