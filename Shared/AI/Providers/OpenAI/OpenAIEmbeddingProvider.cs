namespace Shared.AI.Providers.OpenAI;

using Microsoft.Extensions.Logging;
using Shared.AI.Abstractions;
using System.Text.Json;

/// <summary>
/// OpenAI Embedding Provider implementation.
/// Supports text-embedding-3-small, text-embedding-3-large, text-embedding-ada-002.
/// </summary>
public class OpenAIEmbeddingProvider : IEmbeddingProvider
{
    private readonly IEmbeddingProviderConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;
    private bool _disposed;
    private int? _cachedDimensions;

    private const string ApiBaseUrl = "https://api.openai.com/v1";
    private const string EmbeddingEndpoint = "/embeddings";

    public IEmbeddingProviderConfig Config => _config;

    /// <summary>
    /// Initializes a new instance of the OpenAI embedding provider.
    /// </summary>
    public OpenAIEmbeddingProvider(
        IEmbeddingProviderConfig config,
        HttpClient? httpClient = null,
        ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(config.ApiKey))
            throw new ArgumentException("OpenAI API key is required", nameof(config));

        _config = config;
        _httpClient = httpClient ?? new HttpClient();
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Shared.AI.OpenAIEmbeddingProvider/1.0");
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

        var request = new OpenAIEmbeddingRequest
        {
            Model = _config.Model,
            Input = texts.ToList()
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var endpoint = _config.Endpoint ?? ApiBaseUrl;
        var url = $"{endpoint}{EmbeddingEndpoint}";

        _logger?.LogDebug("OpenAI embedding request for {Count} texts", texts.Count);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(60));

            var response = await _httpClient.PostAsync(url, requestContent, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                _logger?.LogError("OpenAI embedding API error: {StatusCode}", response.StatusCode);
                throw new HttpRequestException($"OpenAI error: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            var result = JsonSerializer.Deserialize<OpenAIEmbeddingResponse>(content)
                ?? throw new InvalidOperationException("Invalid response from OpenAI");

            if (result.Data == null || result.Data.Count == 0)
                throw new InvalidOperationException("No embeddings in response");

            _cachedDimensions = result.Data[0].Embedding?.Length;

            var embeddings = result.Data
                .OrderBy(x => x.Index)
                .Select(d => new Embedding(
                    new ReadOnlyMemory<float>(d.Embedding ?? Array.Empty<float>()),
                    result.Model,
                    d.Embedding?.Length))
                .ToList();

            _logger?.LogDebug("OpenAI embedding succeeded. Dimensions: {Dimensions}", _cachedDimensions);

            return embeddings;
        }
        catch (OperationCanceledException ex)
        {
            _logger?.LogError(ex, "OpenAI embedding request timeout");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "OpenAI embedding HTTP error");
            throw;
        }
    }

    public async Task<int> GetDimensionsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedDimensions.HasValue)
            return _cachedDimensions.Value;

        if (_config.Dimensions.HasValue)
            return _config.Dimensions.Value;

        // Query for dimensions
        try
        {
            var embedding = await EmbedAsync("test", cancellationToken);
            _cachedDimensions = embedding.Dimensions;
            return _cachedDimensions.Value;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to determine embedding dimensions");
            return _config.Dimensions ?? 1536; // Default for text-embedding-3-small
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            await EmbedAsync("health check", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "OpenAI embedding health check failed");
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
            throw new ObjectDisposedException(nameof(OpenAIEmbeddingProvider));
    }

    #region OpenAI API Models

    private class OpenAIEmbeddingRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("input")]
        public List<string> Input { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("encoding_format")]
        public string EncodingFormat { get; set; } = "float";
    }

    private class OpenAIEmbeddingResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("object")]
        public string? Object { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public List<OpenAIEmbeddingData>? Data { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("model")]
        public string? Model { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("usage")]
        public OpenAIEmbeddingUsage? Usage { get; set; }
    }

    private class OpenAIEmbeddingData
    {
        [System.Text.Json.Serialization.JsonPropertyName("object")]
        public string? Object { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("index")]
        public int Index { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }

    private class OpenAIEmbeddingUsage
    {
        [System.Text.Json.Serialization.JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    #endregion
}
