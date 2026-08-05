namespace Shared.AI.Providers.OpenAI;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.AI.Abstractions;

/// <summary>
/// OpenAI LLM Provider implementation.
/// Supports ChatGPT and other OpenAI models.
/// Uses the OpenAI API v1.
/// </summary>
public class OpenAILLMProvider : ILLMProvider
{
    private readonly ILLMProviderConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;
    private bool _disposed;

    private const string ApiBaseUrl = "https://api.openai.com/v1";
    private const string CompletionEndpoint = "/chat/completions";

    public ILLMProviderConfig Config => _config;

    /// <summary>
    /// Initializes a new instance of the OpenAI provider.
    /// </summary>
    public OpenAILLMProvider(
        ILLMProviderConfig config,
        HttpClient? httpClient = null,
        ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(config.ApiKey))
            throw new ArgumentException("OpenAI API key is required", nameof(config));

        _config = config;
        _httpClient = httpClient ?? new HttpClient();
        _logger = logger;

        // Setup default headers
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Shared.AI.OpenAIProvider/1.0");
    }

    public async Task<ChatResponse> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var request = CreateCompletionRequest(messages, options);
        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var endpoint = _config.Endpoint ?? ApiBaseUrl;
        var url = $"{endpoint}{CompletionEndpoint}";

        _logger?.LogDebug("OpenAI completion request to {Endpoint}", url);

        try
        {
            var timeout = options?.Timeout ?? TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var response = await _httpClient.PostAsync(url, requestContent, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                _logger?.LogError("OpenAI API error: {StatusCode} - {Content}", response.StatusCode, errorContent);

                throw new HttpRequestException(
                    $"OpenAI API returned {response.StatusCode}: {errorContent}",
                    null,
                    response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            var result = JsonSerializer.Deserialize<OpenAICompletionResponse>(content)
                ?? throw new InvalidOperationException("Invalid response from OpenAI");

            var message = result.Choices?.FirstOrDefault()?.Message?.Content
                ?? throw new InvalidOperationException("No content in OpenAI response");

            _logger?.LogDebug("OpenAI completion succeeded. Tokens: in={In}, out={Out}",
                result.Usage?.PromptTokens, result.Usage?.CompletionTokens);

            return new ChatResponse(
                message,
                result.Model,
                result.Usage?.PromptTokens,
                result.Usage?.CompletionTokens);
        }
        catch (OperationCanceledException ex)
        {
            _logger?.LogError(ex, "OpenAI request timeout");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "OpenAI HTTP error");
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamCompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        System.Collections.Generic.CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var request = CreateCompletionRequest(messages, options, stream: true);
        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var endpoint = _config.Endpoint ?? ApiBaseUrl;
        var url = $"{endpoint}{CompletionEndpoint}";

        _logger?.LogDebug("OpenAI streaming request to {Endpoint}", url);

        var timeout = options?.Timeout ?? TimeSpan.FromSeconds(60);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        using var response = await _httpClient.PostAsync(url, requestContent, cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
            _logger?.LogError("OpenAI streaming error: {StatusCode}", response.StatusCode);
            throw new HttpRequestException($"OpenAI error: {response.StatusCode}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cts.Token)) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || line == "data: [DONE]")
                continue;

            if (!line.StartsWith("data: "))
                continue;

            var jsonLine = line.Substring("data: ".Length);

            try
            {
                var chunk = JsonSerializer.Deserialize<OpenAIStreamChunk>(jsonLine);
                if (chunk?.Choices?.FirstOrDefault()?.Delta?.Content != null)
                {
                    yield return chunk.Choices[0].Delta.Content;
                }
            }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "Failed to parse streaming chunk");
            }
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            // Make a simple completion request
            var request = CreateCompletionRequest(
                new[] { ChatMessage.User("test") },
                new ChatCompletionOptions { MaxTokens = 1 });

            var requestContent = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var endpoint = _config.Endpoint ?? ApiBaseUrl;
            var url = $"{endpoint}{CompletionEndpoint}";

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.PostAsync(url, requestContent, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "OpenAI health check failed");
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

    private OpenAICompletionRequest CreateCompletionRequest(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options,
        bool stream = false)
    {
        return new OpenAICompletionRequest
        {
            Model = _config.Model,
            Messages = messages.Select(m => new OpenAIMessage
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
            Temperature = options?.Temperature ?? _config.Temperature,
            MaxTokens = options?.MaxTokens ?? _config.MaxTokens,
            TopP = options?.TopP ?? _config.TopP,
            FrequencyPenalty = options?.FrequencyPenalty,
            PresencePenalty = options?.PresencePenalty,
            Stop = options?.StopSequences,
            Stream = stream
        };
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(OpenAILLMProvider));
    }

    #region OpenAI API Models

    private class OpenAICompletionRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("messages")]
        public List<OpenAIMessage> Messages { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("temperature")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("max_tokens")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxTokens { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("top_p")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public double? TopP { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("frequency_penalty")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public double? FrequencyPenalty { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("presence_penalty")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public double? PresencePenalty { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("stop")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<string>? Stop { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private class OpenAIMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class OpenAICompletionResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("object")]
        public string? Object { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("created")]
        public long? Created { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("model")]
        public string? Model { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("choices")]
        public List<OpenAIChoice>? Choices { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("usage")]
        public OpenAIUsage? Usage { get; set; }
    }

    private class OpenAIChoice
    {
        [System.Text.Json.Serialization.JsonPropertyName("index")]
        public int Index { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public OpenAIChoiceMessage? Message { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private class OpenAIChoiceMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("role")]
        public string? Role { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private class OpenAIUsage
    {
        [System.Text.Json.Serialization.JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    private class OpenAIStreamChunk
    {
        [System.Text.Json.Serialization.JsonPropertyName("choices")]
        public List<OpenAIStreamChoice>? Choices { get; set; }
    }

    private class OpenAIStreamChoice
    {
        [System.Text.Json.Serialization.JsonPropertyName("delta")]
        public OpenAIStreamDelta? Delta { get; set; }
    }

    private class OpenAIStreamDelta
    {
        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    #endregion
}
