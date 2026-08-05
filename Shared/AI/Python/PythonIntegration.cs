namespace Shared.AI.Python;

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

/// <summary>
/// Python process manager for executing Python scripts and libraries.
/// Supports virtual environments, JSON IPC, and process pooling.
/// </summary>
public class PythonProcessManager : IAsyncDisposable
{
    private readonly string _pythonPath;
    private readonly string? _virtualEnvironmentPath;
    private readonly Dictionary<string, Process> _workerProcesses = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger? _logger;
    private bool _disposed;

    public PythonProcessManager(
        string? pythonPath = null,
        string? virtualEnvironmentPath = null,
        ILogger? logger = null)
    {
        _pythonPath = pythonPath ?? FindPythonExecutable();
        _virtualEnvironmentPath = virtualEnvironmentPath ?? DetectVirtualEnvironment();
        _logger = logger;

        if (!System.IO.File.Exists(_pythonPath))
            throw new FileNotFoundException($"Python executable not found: {_pythonPath}");

        _logger?.LogInformation("Python process manager initialized. Python: {Path}, venv: {Venv}",
            _pythonPath, _virtualEnvironmentPath ?? "none");
    }

    /// <summary>
    /// Executes a Python script and returns the output.
    /// </summary>
    public async Task<string> ExecuteScriptAsync(
        string scriptContent,
        Dictionary<string, object>? context = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"script_{Guid.NewGuid()}.py");

        try
        {
            // Write context to file if provided
            if (context != null)
            {
                var contextJson = JsonSerializer.Serialize(context);
                scriptContent = $"import json\ncontext = {contextJson}\n\n{scriptContent}";
            }

            await System.IO.File.WriteAllTextAsync(tempFile, scriptContent, cancellationToken);

            return await ExecutePythonAsync(tempFile, timeout, cancellationToken);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
                System.IO.File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Executes a Python module/library function.
    /// </summary>
    public async Task<string> ExecuteModuleAsync(
        string moduleName,
        string functionName,
        Dictionary<string, object>? arguments = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var argsJson = JsonSerializer.Serialize(arguments ?? new());
        var script = $@"
import json
import {moduleName}

result = {moduleName}.{functionName}(**json.loads('{argsJson}'))
print(json.dumps(result))
";

        return await ExecuteScriptAsync(script, null, timeout, cancellationToken);
    }

    /// <summary>
    /// Gets available Python packages.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetInstalledPackagesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var script = "import pip; print(json.dumps([d.key for d in pip._internal.commands.list.get_installed_distributions()]))";
        var output = await ExecuteScriptAsync(script, cancellationToken: cancellationToken);

        try
        {
            return JsonSerializer.Deserialize<List<string>>(output) ?? new();
        }
        catch
        {
            _logger?.LogWarning("Failed to parse installed packages list");
            return new List<string>();
        }
    }

    /// <summary>
    /// Activates a virtual environment for subsequent calls.
    /// </summary>
    public void SetVirtualEnvironment(string venvPath)
    {
        if (!System.IO.Directory.Exists(venvPath))
            throw new DirectoryNotFoundException($"Virtual environment not found: {venvPath}");

        _lock.EnterWriteLock();
        try
        {
            var pythonInVenv = System.IO.Path.Combine(
                venvPath,
                "Scripts",
                "python.exe");

            if (!System.IO.File.Exists(pythonInVenv))
                throw new FileNotFoundException($"Python executable not found in venv: {pythonInVenv}");

            _logger?.LogInformation("Virtual environment activated: {Path}", venvPath);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private async Task<string> ExecutePythonAsync(
        string scriptPath,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _pythonPath,
            Arguments = scriptPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Activate virtual environment if needed
        if (!string.IsNullOrEmpty(_virtualEnvironmentPath))
        {
            var venvPath = _virtualEnvironmentPath;
            var pythonPath = System.IO.Path.Combine(venvPath, "Scripts", "python.exe");
            if (System.IO.File.Exists(pythonPath))
                psi.FileName = pythonPath;
        }

        _logger?.LogDebug("Executing Python script: {Script}", System.IO.Path.GetFileName(scriptPath));

        try
        {
            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start Python process");

            var timeoutMs = (int)(timeout?.TotalMilliseconds ?? 30000);
            var completed = process.WaitForExit(timeoutMs);

            if (!completed)
            {
                process.Kill();
                throw new TimeoutException($"Python script timed out after {timeoutMs}ms");
            }

            if (process.ExitCode != 0)
            {
                var errorOutput = await process.StandardError.ReadToEndAsync();
                _logger?.LogError("Python script failed: {Error}", errorOutput);
                throw new InvalidOperationException($"Python script failed: {errorOutput}");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            return output.Trim();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Python execution failed");
            throw;
        }
    }

    private static string FindPythonExecutable()
    {
        var candidates = new[]
        {
            "python.exe",
            "python3.exe",
            "python",
            "python3"
        };

        foreach (var candidate in candidates)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = candidate,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process?.WaitForExit(1000) == true)
                    return candidate;
            }
            catch { }
        }

        throw new InvalidOperationException("Python executable not found. Please install Python or set pythonPath.");
    }

    private static string? DetectVirtualEnvironment()
    {
        var candidates = new[]
        {
            "venv",
            ".venv",
            "env",
            ".env"
        };

        foreach (var venv in candidates)
        {
            if (System.IO.Directory.Exists(venv))
                return System.IO.Path.GetFullPath(venv);
        }

        return null;
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _lock.EnterWriteLock();
            try
            {
                foreach (var process in _workerProcesses.Values)
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill();
                        process.Dispose();
                    }
                    catch { }
                }

                _workerProcesses.Clear();
                _disposed = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PythonProcessManager));
    }
}

/// <summary>
/// Wrapper for Python transformers library.
/// </summary>
public class TransformersWrapper
{
    private readonly PythonProcessManager _pythonManager;
    private readonly ILogger? _logger;

    public TransformersWrapper(PythonProcessManager pythonManager, ILogger? logger = null)
    {
        _pythonManager = pythonManager ?? throw new ArgumentNullException(nameof(pythonManager));
        _logger = logger;
    }

    /// <summary>
    /// Runs text classification using transformers.
    /// </summary>
    public async Task<string> ClassifyTextAsync(
        string text,
        string model = "distilbert-base-uncased-finetuned-sst-2-english",
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Classifying text with model: {Model}", model);

        var script = $@"
from transformers import pipeline

clf = pipeline('sentiment-analysis', model='{model}')
result = clf('{text}')
print(result[0])
";

        return await _pythonManager.ExecuteScriptAsync(script, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Generates embeddings using sentence transformers.
    /// </summary>
    public async Task<float[]> GetEmbeddingAsync(
        string text,
        string model = "sentence-transformers/all-MiniLM-L6-v2",
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Getting embedding with model: {Model}", model);

        var script = $@"
from sentence_transformers import SentenceTransformer
import json

model = SentenceTransformer('{model}')
embedding = model.encode('{text}')
print(json.dumps(embedding.tolist()))
";

        var output = await _pythonManager.ExecuteScriptAsync(script, cancellationToken: cancellationToken);

        try
        {
            return JsonSerializer.Deserialize<float[]>(output) ?? Array.Empty<float>();
        }
        catch
        {
            _logger?.LogWarning("Failed to parse embedding output");
            return Array.Empty<float>();
        }
    }
}

/// <summary>
/// Wrapper for spaCy NLP library.
/// </summary>
public class spaCyWrapper
{
    private readonly PythonProcessManager _pythonManager;
    private readonly ILogger? _logger;

    public spaCyWrapper(PythonProcessManager pythonManager, ILogger? logger = null)
    {
        _pythonManager = pythonManager ?? throw new ArgumentNullException(nameof(pythonManager));
        _logger = logger;
    }

    /// <summary>
    /// Extracts named entities from text.
    /// </summary>
    public async Task<Dictionary<string, List<string>>> ExtractEntitiesAsync(
        string text,
        string model = "en_core_web_sm",
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Extracting entities with model: {Model}", model);

        var script = $@"
import spacy
import json

nlp = spacy.load('{model}')
doc = nlp('{text}')
entities = {{}}
for ent in doc.ents:
    if ent.label_ not in entities:
        entities[ent.label_] = []
    entities[ent.label_].append(ent.text)
print(json.dumps(entities))
";

        var output = await _pythonManager.ExecuteScriptAsync(script, cancellationToken: cancellationToken);

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(output) ?? new();
        }
        catch
        {
            _logger?.LogWarning("Failed to parse entities output");
            return new();
        }
    }
}
