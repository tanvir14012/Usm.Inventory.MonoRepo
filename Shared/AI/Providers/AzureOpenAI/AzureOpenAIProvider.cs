namespace Shared.AI.Providers.AzureOpenAI;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.AI.Abstractions;

/// <summary>
/// Azure OpenAI LLM Provider implementation.
/// Uses Azure's managed OpenAI API with key-based authentication.
/// </summary>
public class AzureOpenAILLMProvider : ILLMProvider
{
    private readonly ILLMProviderConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;
    private bool _disposed;

    private const string ApiVersion = "2024-02-15-preview";

    public ILLMProviderConfig Config => _config;

    /// <summary>
    /// Initializes a new instance of the Azure OpenAI provider.
    /// Requires: ApiKey and Endpoint (resource URL).
    /// Model should be the deployment name (e.g., "gpt-4-deployment").
    /// </summary>
    public AzureOpenAILLMProvider(
        ILLMProviderConfig config,
        HttpClient? httpClient = null,
        ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(config.ApiKey))
            throw new ArgumentException("Azure API key is required", nameof(config));

        if (string.IsNullOrEmpty(config.Endpoint))
            throw new ArgumentException("Azure endpoint URL is required", nameof(config));

        _config = config;
        _httpClient = httpClient ?? new HttpClient();
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Add("api-key", config.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Shared.AI.AzureOpenAIProvider/1.0");
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

        var endpoint = _config.Endpoint!.TrimEnd('/');
        var url = $"{endpoint}/openai/deployments/{_config.Model}/chat/completions?api-version={ApiVersion}";

        _logger?.LogDebug("Azure OpenAI completion request to {Endpoint}", endpoint);

        try
        {
            var timeout = options?.Timeout ?? TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var response = await _httpClient.PostAsync(url, requestContent, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                _logger?.LogError("Azure OpenAI API error: {StatusCode}", response.StatusCode);
                throw new HttpRequestException($"Azure OpenAI error: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            var result = JsonSerializer.Deserialize<AzureOpenAICompletionResponse>(content)
                ?? throw new InvalidOperationException("Invalid response from Azure OpenAI");

            var message = result.Choices?.FirstOrDefault()?.Message?.Content
                ?? throw new InvalidOperationException("No content in response");

            _logger?.LogDebug("Azure OpenAI completion succeeded. Tokens: in={In}, out={Out}",
                result.Usage?.PromptTokens, result.Usage?.CompletionTokens);

            return new ChatResponse(
                message,
                _config.Model,
                result.Usage?.PromptTokens,
                result.Usage?.CompletionTokens);
        }
        catch (OperationCanceledException ex)
        {
            _logger?.LogError(ex, "Azure OpenAI request timeout");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "Azure OpenAI HTTP error");
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamCompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var request = CreateCompletionRequest(messages, options, stream: true);
        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var endpoint = _config.Endpoint!.TrimEnd('/');
        var url = $"{endpoint}/openai/deployments/{_config.Model}/chat/completions?api-version={ApiVersion}";

        _logger?.LogDebug("Azure OpenAI streaming request");

        var timeout = options?.Timeout ?? TimeSpan.FromSeconds(60);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        using var response = await _httpClient.PostAsync(url, requestContent, cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogError("Azure OpenAI streaming error: {StatusCode}", response.StatusCode);
            throw new HttpRequestException($"Azure OpenAI error: {response.StatusCode}");
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

            var chunk = JsonSerializer.Deserialize<AzureOpenAIStreamChunk>(jsonLine);
            if (chunk?.Choices?.FirstOrDefault()?.Delta?.Content != null)
            {
                yield return chunk.Choices[0].Delta.Content;
            }
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var request = CreateCompletionRequest(
                new[] { ChatMessage.User("test") },
                new ChatCompletionOptions { MaxTokens = 1 });

            var requestContent = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var endpoint = _config.Endpoint!.TrimEnd('/');
            var url = $"{endpoint}/openai/deployments/{_config.Model}/chat/completions?api-version={ApiVersion}";

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.PostAsync(url, requestContent, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Azure OpenAI health check failed");
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

    private AzureOpenAICompletionRequest CreateCompletionRequest(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options,
        bool stream = false)
    {
        return new AzureOpenAICompletionRequest
        {
            Messages = messages.Select(m => new AzureOpenAIMessage
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
            throw new ObjectDisposedException(nameof(AzureOpenAILLMProvider));
    }

    #region Azure OpenAI API Models

    private class AzureOpenAICompletionRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("messages")]
        public List<AzureOpenAIMessage> Messages { get; set; } = new();

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

    private class AzureOpenAIMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class AzureOpenAICompletionResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("choices")]
        public List<AzureOpenAIChoice>? Choices { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("usage")]
        public AzureOpenAIUsage? Usage { get; set; }
    }

    private class AzureOpenAIChoice
    {
        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public AzureOpenAIChoiceMessage? Message { get; set; }
    }

    private class AzureOpenAIChoiceMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private class AzureOpenAIUsage
    {
        [System.Text.Json.Serialization.JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
    }

    private class AzureOpenAIStreamChunk
    {
        [System.Text.Json.Serialization.JsonPropertyName("choices")]
        public List<AzureOpenAIStreamChoice>? Choices { get; set; }
    }

    private class AzureOpenAIStreamChoice
    {
        [System.Text.Json.Serialization.JsonPropertyName("delta")]
        public AzureOpenAIStreamDelta? Delta { get; set; }
    }

    private class AzureOpenAIStreamDelta
    {
        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    #endregion
}
