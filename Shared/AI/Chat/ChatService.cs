namespace Shared.AI.Chat;

using Microsoft.Extensions.Logging;
using Shared.AI.Abstractions;

/// <summary>
/// Default implementation of IChatService.
/// Delegates to an underlying LLM provider.
/// </summary>
public class ChatService : IChatService
{
    private readonly ILLMProvider _provider;
    private readonly ILogger? _logger;
    private bool _disposed;

    public ChatService(ILLMProvider provider, ILogger? logger = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _logger = logger;
    }

    public async Task<ChatResponse> SendAsync(
        string message,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(new[] { ChatMessage.User(message) }, options, cancellationToken);
    }

    public async Task<ChatResponse> SendAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger?.LogDebug("ChatService: Sending {MessageCount} messages", messages.Count);

        return await _provider.CompleteAsync(messages, options, cancellationToken);
    }

    public IAsyncEnumerable<string> StreamAsync(
        string message,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return StreamAsync(new[] { ChatMessage.User(message) }, options, cancellationToken);
    }

    public IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger?.LogDebug("ChatService: Streaming {MessageCount} messages", messages.Count);

        return _provider.StreamCompleteAsync(messages, options, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _provider?.DisposeAsync().GetAwaiter().GetResult();
            _disposed = true;
        }

        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ChatService));
    }
}

/// <summary>
/// Default implementation of IToolRegistry.
/// Thread-safe registry for managing tools.
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly ReaderWriterLockSlim _lock = new();
    private bool _disposed;

    public void RegisterTool(ITool tool)
    {
        if (tool == null) throw new ArgumentNullException(nameof(tool));

        _lock.EnterWriteLock();
        try
        {
            _tools[tool.Name] = tool;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void RegisterTools(IEnumerable<ITool> tools)
    {
        if (tools == null) throw new ArgumentNullException(nameof(tools));

        _lock.EnterWriteLock();
        try
        {
            foreach (var tool in tools)
            {
                _tools[tool.Name] = tool;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool RemoveTool(string toolName)
    {
        if (string.IsNullOrEmpty(toolName)) throw new ArgumentNullException(nameof(toolName));

        _lock.EnterWriteLock();
        try
        {
            return _tools.Remove(toolName);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public ITool? GetTool(string toolName)
    {
        if (string.IsNullOrEmpty(toolName)) return null;

        _lock.EnterReadLock();
        try
        {
            _tools.TryGetValue(toolName, out var tool);
            return tool;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IReadOnlyList<ITool> GetAllTools()
    {
        _lock.EnterReadLock();
        try
        {
            return _tools.Values.ToList().AsReadOnly();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool HasTool(string toolName)
    {
        if (string.IsNullOrEmpty(toolName)) return false;

        _lock.EnterReadLock();
        try
        {
            return _tools.ContainsKey(toolName);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public async Task<string> ExecuteToolAsync(
        string toolName,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        var tool = GetTool(toolName);
        if (tool == null)
            throw new InvalidOperationException($"Tool '{toolName}' not found");

        return await tool.ExecuteAsync(arguments, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _lock?.Dispose();
            _disposed = true;
        }

        return ValueTask.CompletedTask;
    }
}
