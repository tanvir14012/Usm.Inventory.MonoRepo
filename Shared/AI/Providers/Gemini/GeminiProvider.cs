namespace Shared.AI.Providers.Gemini;

using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Google Gemini API provider for chat completion and embeddings.
/// Supports Gemini 1.5 Pro, Flash, and embedding models.
/// </summary>
public class GeminiLLMProvider : ILLMProvider
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _endpoint = "https://generativelanguage.googleapis.com/v1beta/models";
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;

    public string Name => "Gemini";

    public GeminiLLMProvider(
        string apiKey,
        string model = "gemini-1.5-flash",
        HttpClient? httpClient = null,
        ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentException("API key is required", nameof(apiKey));

        _apiKey = apiKey;
        _model = model;
        _httpClient = httpClient ?? new HttpClient();
        _logger = logger;
    }

    public async Task<ChatResponse> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Gemini completion requested with {MessageCount} messages", messages.Count);

        var request = BuildGeminiRequest(messages, options);
        var jsonRequest = System.Text.Json.JsonSerializer.Serialize(request);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post,
            $"{_endpoint}/{_model}:generateContent?key={_apiKey}")
        {
            Content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = System.Text.Json.JsonSerializer.Deserialize<GeminiResponse>(content);

            var responseText = result?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? string.Empty;

            _logger?.LogDebug("Gemini response received: {Length} characters", responseText.Length);

            return new ChatResponse
            {
                Content = responseText,
                Model = _model,
                FinishReason = result?.Candidates?[0]?.FinishReason ?? "unknown",
                TokensUsed = result?.UsageMetadata?.TotalTokenCount ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Gemini API error");
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamCompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Gemini streaming completion requested");

        var request = BuildGeminiRequest(messages, options);
        request["StreamingOptions"] = new { AutomaticFunctionCalling = false };

        var jsonRequest = System.Text.Json.JsonSerializer.Serialize(request);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post,
            $"{_endpoint}/{_model}:streamGenerateContent?key={_apiKey}&alt=sse")
        {
            Content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json")
        };

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, 
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new System.IO.StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                    continue;

                var jsonData = line["data: ".Length..];

                try
                {
                    var chunk = System.Text.Json.JsonSerializer.Deserialize<GeminiResponse>(jsonData);
                    var text = chunk?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? string.Empty;

                    if (!string.IsNullOrEmpty(text))
                        yield return text;
                }
                catch (System.Text.Json.JsonException)
                {
                    _logger?.LogDebug("Failed to parse streaming chunk");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Gemini streaming error");
            throw;
        }
    }

    private Dictionary<string, object> BuildGeminiRequest(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null)
    {
        var contents = messages
            .GroupBy(m => m.Role)
            .Select(g => new
            {
                role = g.Key == ChatMessageRole.User ? "user" : "model",
                parts = g.Select(m => new { text = m.Content }).ToArray()
            })
            .ToList();

        var generationConfig = new
        {
            temperature = options?.Temperature ?? 0.7,
            topP = options?.TopP ?? 0.95,
            topK = options?.TopK ?? 40,
            maxOutputTokens = options?.MaxTokens ?? 2048,
            responseMimeType = "text/plain"
        };

        return new Dictionary<string, object>
        {
            ["contents"] = contents,
            ["generationConfig"] = generationConfig,
            ["safetySettings"] = new[] {
                new { category = "HARM_CATEGORY_UNSPECIFIED", threshold = "BLOCK_NONE" }
            }
        };
    }

    #region Gemini Models

    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public GeminiCandidate[]? Candidates { get; set; }

        [JsonPropertyName("usageMetadata")]
        public GeminiUsageMetadata? UsageMetadata { get; set; }
    }

    private class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }

        [JsonPropertyName("finishReason")]
        public string? FinishReason { get; set; }
    }

    private class GeminiContent
    {
        [JsonPropertyName("parts")]
        public GeminiPart[]? Parts { get; set; }
    }

    private class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private class GeminiUsageMetadata
    {
        [JsonPropertyName("promptTokenCount")]
        public int PromptTokenCount { get; set; }

        [JsonPropertyName("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }

        [JsonPropertyName("totalTokenCount")]
        public int TotalTokenCount { get; set; }
    }

    #endregion
}

/// <summary>
/// Google embedding provider for text-embedding-004.
/// </summary>
public class GeminiEmbeddingProvider : IEmbeddingProvider
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _endpoint = "https://generativelanguage.googleapis.com/v1beta/models";
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;
    private readonly Dictionary<string, int> _dimensionCache = new();

    public string Name => "Gemini Embeddings";

    public GeminiEmbeddingProvider(
        string apiKey,
        string model = "text-embedding-004",
        HttpClient? httpClient = null,
        ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentException("API key is required", nameof(apiKey));

        _apiKey = apiKey;
        _model = model;
        _httpClient = httpClient ?? new HttpClient();
        _logger = logger;
    }

    public async Task<Embedding> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Embedding text of length {Length}", text.Length);

        var request = new { text };
        var jsonRequest = System.Text.Json.JsonSerializer.Serialize(request);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post,
            $"{_endpoint}/{_model}:embedContent?key={_apiKey}")
        {
            Content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = System.Text.Json.JsonSerializer.Deserialize<GeminiEmbeddingResponse>(content);

            var vector = result?.Embedding?.Values ?? Array.Empty<float>();

            _logger?.LogDebug("Embedding generated with {Dimensions} dimensions", vector.Length);

            return new Embedding
            {
                Vector = vector,
                Model = _model,
                Metadata = new Dictionary<string, object> { ["text_length"] = text.Length }
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Gemini embedding error");
            throw;
        }
    }

    public async Task<IReadOnlyList<Embedding>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<Embedding>();

        // Batch in groups of 100 for Gemini API
        const int batchSize = 100;
        for (int i = 0; i < texts.Count; i += batchSize)
        {
            var batch = texts.Skip(i).Take(batchSize).ToList();
            var batchRequest = new { requests = batch.Select(t => new { text = t }).ToList() };
            var jsonRequest = System.Text.Json.JsonSerializer.Serialize(batchRequest);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                $"{_endpoint}/{_model}:batchEmbedContent?key={_apiKey}")
            {
                Content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = System.Text.Json.JsonSerializer.Deserialize<GeminiBatchEmbeddingResponse>(content);

                if (result?.Embeddings != null)
                {
                    embeddings.AddRange(result.Embeddings.Select((e, idx) => new Embedding
                    {
                        Vector = e.Values,
                        Model = _model,
                        Metadata = new Dictionary<string, object> { ["index"] = i + idx }
                    }));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Gemini batch embedding error");
                throw;
            }
        }

        return embeddings;
    }

    public async Task<int> GetDimensionsAsync(CancellationToken cancellationToken = default)
    {
        if (_dimensionCache.TryGetValue(_model, out var dims))
            return dims;

        var embedding = await EmbedAsync("test", cancellationToken);
        var dimension = embedding.Vector.Length;

        _dimensionCache[_model] = dimension;
        return dimension;
    }

    #region Gemini Models

    private class GeminiEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public GeminiEmbeddingData? Embedding { get; set; }
    }

    private class GeminiEmbeddingData
    {
        [JsonPropertyName("values")]
        public float[]? Values { get; set; }
    }

    private class GeminiBatchEmbeddingResponse
    {
        [JsonPropertyName("embeddings")]
        public GeminiEmbeddingData[]? Embeddings { get; set; }
    }

    #endregion
}
