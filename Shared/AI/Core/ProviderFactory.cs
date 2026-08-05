namespace Shared.AI.Core;

using Microsoft.Extensions.Logging;
using Shared.AI.Abstractions;

/// <summary>
/// Enumerates supported AI providers.
/// </summary>
public enum AIProviderType
{
    OpenAI,
    AzureOpenAI,
    Ollama,
    Gemini,
    Claude,
    SemanticKernel,
    Custom
}

/// <summary>
/// Factory for creating AI providers.
/// Supports lazy initialization and singleton pattern.
/// </summary>
public interface IProviderFactory
{
    /// <summary>
    /// Creates an LLM provider.
    /// </summary>
    Task<ILLMProvider> CreateLLMProviderAsync(
        AIProviderType type,
        ILLMProviderConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an embedding provider.
    /// </summary>
    Task<IEmbeddingProvider> CreateEmbeddingProviderAsync(
        AIProviderType type,
        IEmbeddingProviderConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a custom provider factory.
    /// </summary>
    void RegisterCustomProvider<TProvider>(
        string providerName,
        Func<ILLMProviderConfig, Task<TProvider>> factory)
        where TProvider : ILLMProvider;

    /// <summary>
    /// Registers a custom embedding provider factory.
    /// </summary>
    void RegisterCustomEmbeddingProvider<TProvider>(
        string providerName,
        Func<IEmbeddingProviderConfig, Task<TProvider>> factory)
        where TProvider : IEmbeddingProvider;
}

/// <summary>
/// Base implementation of provider factory.
/// </summary>
public class ProviderFactory : IProviderFactory
{
    private readonly Dictionary<string, Func<ILLMProviderConfig, Task<ILLMProvider>>> _llmFactories;
    private readonly Dictionary<string, Func<IEmbeddingProviderConfig, Task<IEmbeddingProvider>>> _embeddingFactories;
    private readonly ILogger? _logger;

    public ProviderFactory(ILogger? logger = null)
    {
        _logger = logger;
        _llmFactories = new();
        _embeddingFactories = new();
    }

    public async Task<ILLMProvider> CreateLLMProviderAsync(
        AIProviderType type,
        ILLMProviderConfig config,
        CancellationToken cancellationToken = default)
    {
        var typeName = type.ToString();
        if (!_llmFactories.TryGetValue(typeName, out var factory))
        {
            throw new InvalidOperationException($"LLM provider '{typeName}' is not registered");
        }

        _logger?.LogDebug("Creating LLM provider: {ProviderType}", typeName);
        return await factory(config);
    }

    public async Task<IEmbeddingProvider> CreateEmbeddingProviderAsync(
        AIProviderType type,
        IEmbeddingProviderConfig config,
        CancellationToken cancellationToken = default)
    {
        var typeName = type.ToString();
        if (!_embeddingFactories.TryGetValue(typeName, out var factory))
        {
            throw new InvalidOperationException($"Embedding provider '{typeName}' is not registered");
        }

        _logger?.LogDebug("Creating embedding provider: {ProviderType}", typeName);
        return await factory(config);
    }

    public void RegisterCustomProvider<TProvider>(
        string providerName,
        Func<ILLMProviderConfig, Task<TProvider>> factory)
        where TProvider : ILLMProvider
    {
        _llmFactories[providerName] = async config =>
        {
            return await factory(config);
        };

        _logger?.LogDebug("Registered custom LLM provider: {ProviderName}", providerName);
    }

    public void RegisterCustomEmbeddingProvider<TProvider>(
        string providerName,
        Func<IEmbeddingProviderConfig, Task<TProvider>> factory)
        where TProvider : IEmbeddingProvider
    {
        _embeddingFactories[providerName] = async config =>
        {
            return await factory(config);
        };

        _logger?.LogDebug("Registered custom embedding provider: {ProviderName}", providerName);
    }
}

/// <summary>
/// Builder for configuring LLM provider settings.
/// Fluent API for easy configuration.
/// </summary>
public class LLMProviderConfigBuilder
{
    private string? _providerName;
    private string? _model;
    private string? _apiKey;
    private string? _endpoint;
    private double? _temperature;
    private int? _maxTokens;
    private double? _topP;
    private Dictionary<string, string>? _customHeaders;

    public LLMProviderConfigBuilder WithProvider(string providerName)
    {
        _providerName = providerName;
        return this;
    }

    public LLMProviderConfigBuilder WithModel(string model)
    {
        _model = model;
        return this;
    }

    public LLMProviderConfigBuilder WithApiKey(string apiKey)
    {
        _apiKey = apiKey;
        return this;
    }

    public LLMProviderConfigBuilder WithEndpoint(string endpoint)
    {
        _endpoint = endpoint;
        return this;
    }

    public LLMProviderConfigBuilder WithTemperature(double temperature)
    {
        _temperature = temperature;
        return this;
    }

    public LLMProviderConfigBuilder WithMaxTokens(int maxTokens)
    {
        _maxTokens = maxTokens;
        return this;
    }

    public LLMProviderConfigBuilder WithTopP(double topP)
    {
        _topP = topP;
        return this;
    }

    public LLMProviderConfigBuilder WithCustomHeaders(Dictionary<string, string> headers)
    {
        _customHeaders = headers;
        return this;
    }

    public LLMProviderConfigBuilder AddCustomHeader(string key, string value)
    {
        _customHeaders ??= new();
        _customHeaders[key] = value;
        return this;
    }

    public LLMProviderConfigBuilder FromEnvironment(string prefix)
    {
        _apiKey ??= Environment.GetEnvironmentVariable($"{prefix}_API_KEY");
        _endpoint ??= Environment.GetEnvironmentVariable($"{prefix}_ENDPOINT");
        _model ??= Environment.GetEnvironmentVariable($"{prefix}_MODEL");
        return this;
    }

    public ILLMProviderConfig Build()
    {
        if (string.IsNullOrEmpty(_providerName))
            throw new InvalidOperationException("Provider name is required");

        if (string.IsNullOrEmpty(_model))
            throw new InvalidOperationException("Model is required");

        return new LLMProviderConfig(
            _providerName,
            _model,
            _apiKey,
            _endpoint,
            _temperature,
            _maxTokens,
            _topP,
            _customHeaders);
    }

    private class LLMProviderConfig : ILLMProviderConfig
    {
        public LLMProviderConfig(
            string providerName,
            string model,
            string? apiKey,
            string? endpoint,
            double? temperature,
            int? maxTokens,
            double? topP,
            Dictionary<string, string>? customHeaders)
        {
            ProviderName = providerName;
            Model = model;
            ApiKey = apiKey;
            Endpoint = endpoint;
            Temperature = temperature;
            MaxTokens = maxTokens;
            TopP = topP;
            CustomHeaders = customHeaders?.AsReadOnly();
        }

        public string ProviderName { get; }
        public string Model { get; }
        public string? ApiKey { get; }
        public string? Endpoint { get; }
        public double? Temperature { get; }
        public int? MaxTokens { get; }
        public double? TopP { get; }
        public IReadOnlyDictionary<string, string>? CustomHeaders { get; }
    }
}

/// <summary>
/// Builder for configuring embedding provider settings.
/// </summary>
public class EmbeddingProviderConfigBuilder
{
    private string? _providerName;
    private string? _model;
    private string? _apiKey;
    private string? _endpoint;
    private int? _dimensions;

    public EmbeddingProviderConfigBuilder WithProvider(string providerName)
    {
        _providerName = providerName;
        return this;
    }

    public EmbeddingProviderConfigBuilder WithModel(string model)
    {
        _model = model;
        return this;
    }

    public EmbeddingProviderConfigBuilder WithApiKey(string apiKey)
    {
        _apiKey = apiKey;
        return this;
    }

    public EmbeddingProviderConfigBuilder WithEndpoint(string endpoint)
    {
        _endpoint = endpoint;
        return this;
    }

    public EmbeddingProviderConfigBuilder WithDimensions(int dimensions)
    {
        _dimensions = dimensions;
        return this;
    }

    public EmbeddingProviderConfigBuilder FromEnvironment(string prefix)
    {
        _apiKey ??= Environment.GetEnvironmentVariable($"{prefix}_API_KEY");
        _endpoint ??= Environment.GetEnvironmentVariable($"{prefix}_ENDPOINT");
        _model ??= Environment.GetEnvironmentVariable($"{prefix}_MODEL");

        if (int.TryParse(Environment.GetEnvironmentVariable($"{prefix}_DIMENSIONS"), out var dims))
            _dimensions ??= dims;

        return this;
    }

    public IEmbeddingProviderConfig Build()
    {
        if (string.IsNullOrEmpty(_providerName))
            throw new InvalidOperationException("Provider name is required");

        if (string.IsNullOrEmpty(_model))
            throw new InvalidOperationException("Model is required");

        return new EmbeddingProviderConfig(
            _providerName,
            _model,
            _apiKey,
            _endpoint,
            _dimensions);
    }

    private class EmbeddingProviderConfig : IEmbeddingProviderConfig
    {
        public EmbeddingProviderConfig(
            string providerName,
            string model,
            string? apiKey,
            string? endpoint,
            int? dimensions)
        {
            ProviderName = providerName;
            Model = model;
            ApiKey = apiKey;
            Endpoint = endpoint;
            Dimensions = dimensions;
        }

        public string ProviderName { get; }
        public string Model { get; }
        public string? ApiKey { get; }
        public string? Endpoint { get; }
        public int? Dimensions { get; }
    }
}
