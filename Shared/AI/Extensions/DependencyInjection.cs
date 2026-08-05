namespace Shared.AI.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.AI.Abstractions;
using Shared.AI.Chat;
using Shared.AI.Core;
using Shared.AI.Providers.AzureOpenAI;
using Shared.AI.Providers.Claude;
using Shared.AI.Providers.Gemini;
using Shared.AI.Providers.Ollama;
using Shared.AI.Providers.OpenAI;

/// <summary>
/// Dependency injection extension methods for Shared.AI framework.
/// </summary>
public static class AIServiceCollectionExtensions
{
    /// <summary>
    /// Adds the AI framework to the service collection.
    /// </summary>
    public static IServiceCollection AddSharedAI(this IServiceCollection services)
    {
        services.AddSingleton<IProviderFactory, ProviderFactory>();
        services.AddSingleton<IToolRegistry, ToolRegistry>();

        return services;
    }

    /// <summary>
    /// Adds OpenAI provider for LLM.
    /// </summary>
    public static IServiceCollection AddOpenAILLMProvider(
        this IServiceCollection services,
        Action<LLMProviderConfigBuilder> configure)
    {
        var builder = new LLMProviderConfigBuilder();
        builder.WithProvider("OpenAI");
        configure(builder);

        var config = builder.Build();
        services.AddSingleton<ILLMProvider>(sp =>
            new OpenAILLMProvider(config, null, sp.GetService<ILogger<OpenAILLMProvider>>()));

        return services;
    }

    /// <summary>
    /// Adds OpenAI provider for embeddings.
    /// </summary>
    public static IServiceCollection AddOpenAIEmbeddingProvider(
        this IServiceCollection services,
        Action<EmbeddingProviderConfigBuilder> configure)
    {
        var builder = new EmbeddingProviderConfigBuilder();
        builder.WithProvider("OpenAI");
        configure(builder);

        var config = builder.Build();
        services.AddSingleton<IEmbeddingProvider>(sp =>
            new OpenAIEmbeddingProvider(config, null, sp.GetService<ILogger<OpenAIEmbeddingProvider>>()));

        return services;
    }

    /// <summary>
    /// Adds Ollama provider for local LLM.
    /// </summary>
    public static IServiceCollection AddOllamaLLMProvider(
        this IServiceCollection services,
        Action<LLMProviderConfigBuilder> configure)
    {
        var builder = new LLMProviderConfigBuilder();
        builder.WithProvider("Ollama");
        configure(builder);

        var config = builder.Build();
        services.AddSingleton<ILLMProvider>(sp =>
            new OllamaLLMProvider(config, null, sp.GetService<ILogger<OllamaLLMProvider>>()));

        return services;
    }

    /// <summary>
    /// Adds Ollama provider for embeddings.
    /// </summary>
    public static IServiceCollection AddOllamaEmbeddingProvider(
        this IServiceCollection services,
        Action<EmbeddingProviderConfigBuilder> configure)
    {
        var builder = new EmbeddingProviderConfigBuilder();
        builder.WithProvider("Ollama");
        configure(builder);

        var config = builder.Build();
        services.AddSingleton<IEmbeddingProvider>(sp =>
            new OllamaEmbeddingProvider(config, null, sp.GetService<ILogger<OllamaEmbeddingProvider>>()));

        return services;
    }

    /// <summary>
    /// Adds default chat service using the registered LLM provider.
    /// </summary>
    public static IServiceCollection AddChatService(this IServiceCollection services)
    {
        services.AddSingleton<IChatService>(sp =>
        {
            var provider = sp.GetRequiredService<ILLMProvider>();
            var logger = sp.GetService<ILogger<ChatService>>();
            return new ChatService(provider, logger);
        });

        return services;
    }

    /// <summary>
    /// Registers OpenAI provider in the provider factory.
    /// </summary>
    public static IServiceCollection AddProviderFactoryOpenAI(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var factory = sp.GetRequiredService<IProviderFactory>();
            factory.RegisterCustomProvider<OpenAILLMProvider>(
                "OpenAI",
                async config =>
                {
                    var logger = sp.GetService<ILogger<OpenAILLMProvider>>();
                    return await Task.FromResult(new OpenAILLMProvider(config, null, logger));
                });

            factory.RegisterCustomEmbeddingProvider<OpenAIEmbeddingProvider>(
                "OpenAI",
                async config =>
                {
                    var logger = sp.GetService<ILogger<OpenAIEmbeddingProvider>>();
                    return await Task.FromResult(new OpenAIEmbeddingProvider(config, null, logger));
                });

            return factory;
        });

        return services;
    }

    /// <summary>
    /// Registers Ollama provider in the provider factory.
    /// </summary>
    public static IServiceCollection AddProviderFactoryOllama(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var factory = sp.GetRequiredService<IProviderFactory>();
            factory.RegisterCustomProvider<OllamaLLMProvider>(
                "Ollama",
                async config =>
                {
                    var logger = sp.GetService<ILogger<OllamaLLMProvider>>();
                    return await Task.FromResult(new OllamaLLMProvider(config, null, logger));
                });

            factory.RegisterCustomEmbeddingProvider<OllamaEmbeddingProvider>(
                "Ollama",
                async config =>
                {
                    var logger = sp.GetService<ILogger<OllamaEmbeddingProvider>>();
                    return await Task.FromResult(new OllamaEmbeddingProvider(config, null, logger));
                });

            return factory;
        });

        return services;
    }

    /// <summary>
    /// Adds Azure OpenAI provider for LLM.
    /// </summary>
    public static IServiceCollection AddAzureOpenAILLMProvider(
        this IServiceCollection services,
        string endpoint,
        string apiKey,
        string deploymentName,
        string? model = null)
    {
        services.AddSingleton<ILLMProvider>(sp =>
            new AzureOpenAILLMProvider(
                new LLMProviderConfigBuilder()
                    .WithEndpoint(endpoint)
                    .WithApiKey(apiKey)
                    .WithModel(model)
                    .Build(),
                null,
                sp.GetService<ILogger<AzureOpenAILLMProvider>>()));

        return services;
    }

    /// <summary>
    /// Adds Gemini provider for LLM.
    /// </summary>
    public static IServiceCollection AddGeminiLLMProvider(
        this IServiceCollection services,
        string apiKey,
        string model = "gemini-1.5-flash")
    {
        services.AddSingleton<ILLMProvider>(sp =>
            new GeminiLLMProvider(apiKey, model, null, sp.GetService<ILogger<GeminiLLMProvider>>()));

        return services;
    }

    /// <summary>
    /// Adds Gemini provider for embeddings.
    /// </summary>
    public static IServiceCollection AddGeminiEmbeddingProvider(
        this IServiceCollection services,
        string apiKey,
        string model = "text-embedding-004")
    {
        services.AddSingleton<IEmbeddingProvider>(sp =>
            new GeminiEmbeddingProvider(apiKey, model, null, sp.GetService<ILogger<GeminiEmbeddingProvider>>()));

        return services;
    }

    /// <summary>
    /// Adds Claude provider for LLM.
    /// </summary>
    public static IServiceCollection AddClaudeLLMProvider(
        this IServiceCollection services,
        string apiKey,
        string model = "claude-3-sonnet-20240229")
    {
        services.AddSingleton<ILLMProvider>(sp =>
            new ClaudeLLMProvider(apiKey, model, null, sp.GetService<ILogger<ClaudeLLMProvider>>()));

        return services;
    }

    /// <summary>
    /// Adds Claude provider for embeddings (delegates to fallback provider).
    /// </summary>
    public static IServiceCollection AddClaudeEmbeddingProvider(
        this IServiceCollection services,
        IEmbeddingProvider fallbackProvider)
    {
        services.AddSingleton<IEmbeddingProvider>(sp =>
            new ClaudeEmbeddingProvider(fallbackProvider, sp.GetService<ILogger<ClaudeEmbeddingProvider>>()));

        return services;
    }
}

    /// <summary>
    /// Extension methods for building AI services.
    /// </summary>
    public static class AIServiceBuilderExtensions
    {
        /// <summary>
        /// Builds a complete AI service collection with typical defaults.
        /// </summary>
        public static IServiceCollection AddAIFramework(
            this IServiceCollection services,
            Action<AIFrameworkBuilder> configure)
        {
            var builder = new AIFrameworkBuilder(services);
            configure(builder);
            return services;
        }
    }

    /// <summary>
    /// Fluent builder for configuring AI framework.
    /// </summary>
    public class AIFrameworkBuilder
    {
        private readonly IServiceCollection _services;

        public AIFrameworkBuilder(IServiceCollection services)
        {
            _services = services;
            _services.AddSharedAI();
        }

        public AIFrameworkBuilder WithOpenAIProvider(Action<LLMProviderConfigBuilder> configure)
        {
            _services.AddOpenAILLMProvider(configure);
            _services.AddOpenAIEmbeddingProvider(c => c.FromEnvironment("OPENAI"));
            return this;
        }

        public AIFrameworkBuilder WithOllamaProvider(Action<LLMProviderConfigBuilder>? configure = null)
        {
            _services.AddOllamaLLMProvider(b =>
            {
                b.WithModel("llama2");
                configure?.Invoke(b);
            });

            _services.AddOllamaEmbeddingProvider(b => b.WithModel("nomic-embed-text"));
            return this;
        }

        public AIFrameworkBuilder WithAzureOpenAIProvider(
            string endpoint,
            string apiKey,
            string deploymentName,
            string? model = null)
        {
            _services.AddAzureOpenAILLMProvider(endpoint, apiKey, deploymentName, model);
            return this;
        }

        public AIFrameworkBuilder WithGeminiProvider(
            string apiKey,
            string llmModel = "gemini-1.5-flash",
            string embeddingModel = "text-embedding-004")
        {
            _services.AddGeminiLLMProvider(apiKey, llmModel);
            _services.AddGeminiEmbeddingProvider(apiKey, embeddingModel);
            return this;
        }

        public AIFrameworkBuilder WithClaudeProvider(
            string apiKey,
            string model = "claude-3-sonnet-20240229")
        {
            _services.AddClaudeLLMProvider(apiKey, model);
            return this;
        }

        public AIFrameworkBuilder WithChatService()
        {
            _services.AddChatService();
            return this;
        }

        public AIFrameworkBuilder WithProviderFactory()
        {
            _services.AddProviderFactoryOpenAI();
            _services.AddProviderFactoryOllama();
            return this;
        }

        public IServiceCollection Build() => _services;
    }