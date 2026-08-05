namespace Shared.AI.Providers.Ollama;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.AI.Abstractions;

/// <summary>
/// Ollama LLM Provider implementation.
/// Supports local language models via Ollama.
/// Ollama must be running locally (default: http://localhost:11434).
/// </summary>
public class OllamaLLMProvider : ILLMProvider
{
    private readonly ILLMProviderConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;
    private bool _disposed;

    private const string DefaultEndpoint = "http://localhost:11434";
    private const string ApiPath = "/api/chat";

    public ILLMProviderConfig Config => _config;

    /// <summary>
    /// Initializes a new instance of the Ollama provider.
    /// </summary>
    public OllamaLLMProvider(
        ILLMProviderConfig config,
        HttpClient? httpClient = null,
        ILogger? logger = null)
    {
        _config = config;
        _httpClient = httpClient ?? new HttpClient();
        _logger = logger;
    }

    public async Task<ChatResponse> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var request = CreateChatRequest(messages, options);
        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var endpoint = _config.Endpoint ?? DefaultEndpoint;
        var url = $"{endpoint}{ApiPath}";

        _logger?.LogDebug("Ollama completion request to {Endpoint}", endpoint);

        try
        {
            var timeout = options?.Timeout ?? TimeSpan.FromSeconds(120);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var response = await _httpClient.PostAsync(url, requestContent, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogError("Ollama API error: {StatusCode}", response.StatusCode);
                throw new HttpRequestException($"Ollama error: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            var result = JsonSerializer.Deserialize<OllamaChatResponse>(content)
                ?? throw new InvalidOperationException("Invalid response from Ollama");

            var message = result.Message?.Content
                ?? throw new InvalidOperationException("No content in Ollama response");

            _logger?.LogDebug("Ollama completion succeeded");

            return new ChatResponse(message, _config.Model);
        }
        catch (OperationCanceledException ex)
        {
            _logger?.LogError(ex, "Ollama request timeout");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "Ollama HTTP error");
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamCompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var request = CreateChatRequest(messages, options, stream: true);
        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var endpoint = _config.Endpoint ?? DefaultEndpoint;
        var url = $"{endpoint}{ApiPath}";

        _logger?.LogDebug("Ollama streaming request to {Endpoint}", endpoint);

        var timeout = options?.Timeout ?? TimeSpan.FromSeconds(180);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        using var response = await _httpClient.PostAsync(url, requestContent, cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogError("Ollama streaming error: {StatusCode}", response.StatusCode);
            throw new HttpRequestException($"Ollama error: {response.StatusCode}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cts.Token)) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(line);
                if (chunk?.Message?.Content != null)
                {
                    yield return chunk.Message.Content;
                }
            }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "Failed to parse Ollama streaming chunk");
            }
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var endpoint = _config.Endpoint ?? DefaultEndpoint;
            var tagsUrl = $"{endpoint}/api/tags";

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.GetAsync(tagsUrl, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Ollama health check failed");
            return false;
        }
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }

        return ValueTask.CompletedTask;
    }

    private OllamaChatRequest CreateChatRequest(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options,
        bool stream = false)
    {
        return new OllamaChatRequest
        {
            Model = _config.Model,
            Messages = messages.Select(m => new OllamaChatMessage
            {
                Role = m.Role switch
                {
                    MessageRole.System => "system",
                    MessageRole.User => "user",
                    MessageRole.Assistant => "assistant",
                    MessageRole.Tool => "tool",
                    _ => "user"
                },
                Content = m.Content
            }).ToList(),
            Stream = stream,
            Options = new OllamaOptions
            {
                Temperature = options?.Temperature ?? _config.Temperature,
                TopP = options?.TopP ?? _config.TopP,
                TopK = options?.TopK
            }
        };
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(OllamaLLMProvider));
    }

    #region Ollama API Models

    private class OllamaChatRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("messages")]
        public List<OllamaChatMessage> Messages { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("options")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public OllamaOptions? Options { get; set; }
    }

    private class OllamaChatMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class OllamaChatResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("model")]
        public string? Model { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public OllamaChatMessage? Message { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("done")]
        public bool Done { get; set; }
    }

    private class OllamaStreamChunk
    {
        [System.Text.Json.Serialization.JsonPropertyName("model")]
        public string? Model { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public OllamaChatMessage? Message { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("done")]
        public bool Done { get; set; }
    }

    private class OllamaOptions
    {
        [System.Text.Json.Serialization.JsonPropertyName("temperature")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("top_p")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public double? TopP { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("top_k")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public int? TopK { get; set; }
    }

    #endregion
}

/// <summary>
/// Ollama Embedding Provider implementation.
/// </summary>
public class OllamaEmbeddingProvider : IEmbeddingProvider
{
    private readonly IEmbeddingProviderConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;
    private bool _disposed;
    private int? _cachedDimensions;

    private const string DefaultEndpoint = "http://localhost:11434";
    private const string ApiPath = "/api/embeddings";

    public IEmbeddingProviderConfig Config => _config;

    public OllamaEmbeddingProvider(
        IEmbeddingProviderConfig config,
        HttpClient? httpClient = null,
        ILogger? logger = null)
    {
        _config = config;
        _httpClient = httpClient ?? new HttpClient();
        _logger = logger;
    }

    public async Task<Embedding> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var embeddings = await EmbedBatchAsync(new[] { text }, cancellationToken);
        return embeddings.FirstOrDefault() ?? throw new InvalidOperationException("No embeddings returned");
    }

    public async Task<IReadOnlyList<Embedding>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var embeddings = new List<Embedding>();

        foreach (var text in texts)
        {
            var request = new OllamaEmbeddingRequest
            {
                Model = _config.Model,
                Prompt = text
            };

            var requestContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var endpoint = _config.Endpoint ?? DefaultEndpoint;
            var url = $"{endpoint}{ApiPath}";

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(60));

                var response = await _httpClient.PostAsync(url, requestContent, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError("Ollama embedding error: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"Ollama error: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync(cts.Token);
                var result = System.Text.Json.JsonSerializer.Deserialize<OllamaEmbeddingResponse>(content)
                    ?? throw new InvalidOperationException("Invalid response from Ollama");

                if (result.Embedding == null || result.Embedding.Length == 0)
                    throw new InvalidOperationException("No embedding in response");

                _cachedDimensions = result.Embedding.Length;

                embeddings.Add(new Embedding(
                    new ReadOnlyMemory<float>(result.Embedding),
                    _config.Model,
                    result.Embedding.Length));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to embed text");
                throw;
            }
        }

        return embeddings;
    }

    public async Task<int> GetDimensionsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedDimensions.HasValue)
            return _cachedDimensions.Value;

        if (_config.Dimensions.HasValue)
            return _config.Dimensions.Value;

        try
        {
            var embedding = await EmbedAsync("test", cancellationToken);
            _cachedDimensions = embedding.Dimensions;
            return _cachedDimensions.Value;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to determine embedding dimensions");
            return _config.Dimensions ?? 384; // Default for nomic-embed-text
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var endpoint = _config.Endpoint ?? DefaultEndpoint;
            var tagsUrl = $"{endpoint}/api/tags";

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.GetAsync(tagsUrl, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Ollama embedding health check failed");
            return false;
        }
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }

        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(OllamaEmbeddingProvider));
    }

    #region Ollama API Models

    private class OllamaEmbeddingRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;
    }

    private class OllamaEmbeddingResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }

    #endregion
}
