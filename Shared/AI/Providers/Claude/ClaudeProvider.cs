namespace Shared.AI.Providers.Claude;

using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Shared.AI.Abstractions;

/// <summary>
/// Anthropic Claude API provider for chat completion.
/// Supports Claude 3 (Opus, Sonnet, Haiku) and Claude 2.1.
/// </summary>
public class ClaudeLLMProvider : ILLMProvider
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _endpoint = "https://api.anthropic.com/v1/messages";
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;
    private const string ApiVersion = "2024-01-15";

    public string Name => "Claude";
    public ILLMProviderConfig Config => new ClaudeProviderConfig(_model);

    public ClaudeLLMProvider(
        string apiKey,
        string model = "claude-3-sonnet-20240229",
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

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async Task<ChatResponse> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Claude completion requested with {MessageCount} messages", messages.Count);

        var request = BuildClaudeRequest(messages, options);
        var jsonRequest = System.Text.Json.JsonSerializer.Serialize(request);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Add("x-api-key", _apiKey);
        httpRequest.Headers.Add("anthropic-version", ApiVersion);

        try
        {
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = System.Text.Json.JsonSerializer.Deserialize<ClaudeResponse>(content);

            var responseText = string.Join("",
                result?.Content?.Where(c => c.Type == "text").Select(c => c.Text) ?? new string[0]);

            _logger?.LogDebug("Claude response received: {Length} characters", responseText.Length);

            return new ChatResponse
            {
                Content = responseText,
                Model = _model,
                FinishReason = result?.StopReason ?? "unknown",
                TokensUsed = (result?.Usage?.OutputTokens ?? 0) + (result?.Usage?.InputTokens ?? 0)
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Claude API error");
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamCompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Claude streaming completion requested");

        var request = BuildClaudeRequest(messages, options);
        request["stream"] = true;

        var jsonRequest = System.Text.Json.JsonSerializer.Serialize(request);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Add("x-api-key", _apiKey);
        httpRequest.Headers.Add("anthropic-version", ApiVersion);

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
                    var chunk = System.Text.Json.JsonSerializer.Deserialize<ClaudeStreamEvent>(jsonData);

                    if (chunk?.Type == "content_block_delta")
                    {
                        var text = chunk.Delta?.Text ?? string.Empty;
                        if (!string.IsNullOrEmpty(text))
                            yield return text;
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    _logger?.LogDebug("Failed to parse streaming chunk");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Claude streaming error");
            throw;
        }
    }

    private Dictionary<string, object> BuildClaudeRequest(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null)
    {
        var systemMessage = messages.FirstOrDefault(m => m.Role == ChatMessageRole.System)?.Content ?? "";

        var claudeMessages = messages
            .Where(m => m.Role != ChatMessageRole.System)
            .Select(m => new
            {
                role = m.Role == ChatMessageRole.User ? "user" : "assistant",
                content = m.Content
            })
            .ToList();

        var request = new Dictionary<string, object>
        {
            ["model"] = _model,
            ["max_tokens"] = options?.MaxTokens ?? 1024,
            ["messages"] = claudeMessages
        };

        if (!string.IsNullOrEmpty(systemMessage))
            request["system"] = systemMessage;

        if (options?.Temperature.HasValue ?? false)
            request["temperature"] = options.Temperature;

        if (options?.TopP.HasValue ?? false)
            request["top_p"] = options.TopP;

        return request;
    }

    #region Claude Models

    private class ClaudeResponse
    {
        [JsonPropertyName("content")]
        public ClaudeContent[]? Content { get; set; }

        [JsonPropertyName("stop_reason")]
        public string? StopReason { get; set; }

        [JsonPropertyName("usage")]
        public ClaudeUsage? Usage { get; set; }
    }

    private class ClaudeContent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private class ClaudeUsage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }

    private class ClaudeStreamEvent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("delta")]
        public ClaudeStreamDelta? Delta { get; set; }
    }

    private class ClaudeStreamDelta
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    #endregion
}

/// <summary>
/// Anthropic Claude embedding provider using Claude's text processing.
/// Note: Claude doesn't have native embeddings; this integrates with external embedding providers.
/// </summary>
public class ClaudeEmbeddingProvider : IEmbeddingProvider
{
    private readonly IEmbeddingProvider _fallbackProvider;
    private readonly ILogger? _logger;

    public string Name => "Claude (via fallback)";
    public IEmbeddingProviderConfig Config => new ClaudeEmbeddingProviderConfig();

    public ClaudeEmbeddingProvider(
        IEmbeddingProvider fallbackProvider,
        ILogger? logger = null)
    {
        _fallbackProvider = fallbackProvider ?? throw new ArgumentNullException(nameof(fallbackProvider));
        _logger = logger;
    }

    public async Task<Embedding> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Delegating embedding to fallback provider");
        return await _fallbackProvider.EmbedAsync(text, cancellationToken);
    }

    public async Task<IReadOnlyList<Embedding>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Delegating batch embedding to fallback provider");
        return await _fallbackProvider.EmbedBatchAsync(texts, cancellationToken);
    }

    public async Task<int> GetDimensionsAsync(CancellationToken cancellationToken = default)
    {
        return await _fallbackProvider.GetDimensionsAsync(cancellationToken);
    }

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

file sealed class ClaudeProviderConfig(string model) : ILLMProviderConfig
{
    public string ProviderName => "Claude";
    public string Model => model;
    public string? ApiKey => null;
    public string? Endpoint => null;
    public double? Temperature => null;
    public int? MaxTokens => null;
    public double? TopP => null;
    public IReadOnlyDictionary<string, string>? CustomHeaders => null;
}

file sealed class ClaudeEmbeddingProviderConfig : IEmbeddingProviderConfig
{
    public string ProviderName => "Claude";
    public string Model => "fallback";
    public string? ApiKey => null;
    public string? Endpoint => null;
    public int? Dimensions => null;
}
