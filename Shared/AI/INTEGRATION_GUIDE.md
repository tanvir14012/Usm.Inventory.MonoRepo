# Shared.AI Integration Guide

Complete examples and patterns for using the Shared.AI framework across your applications.

## Table of Contents
1. [Basic Setup](#basic-setup)
2. [Chat Completions](#chat-completions)
3. [RAG Workflow](#rag-workflow)
4. [Memory Management](#memory-management)
5. [Agents & Tools](#agents--tools)
6. [Embeddings & Search](#embeddings--search)
7. [Error Handling](#error-handling)
8. [Model Selection](#model-selection)
9. [Structured Output](#structured-output)
10. [Complete Application Example](#complete-application-example)

## Basic Setup

### Configure DI in Program.cs

```csharp
// Minimal setup with OpenAI
services.AddAIFramework(builder => 
    builder
        .WithOpenAIProvider(c => c
            .WithModel("gpt-4")
            .FromEnvironment("OPENAI_API_KEY"))
        .WithChatService()
);

// Multi-provider setup
services.AddAIFramework(builder =>
    builder
        .WithOpenAIProvider(c => c
            .WithModel("gpt-4")
            .FromEnvironment("OPENAI_API_KEY"))
        .WithAzureOpenAIProvider(
            endpoint: "https://my-resource.openai.azure.com",
            apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY")!,
            deploymentName: "gpt-4-deployment")
        .WithGeminiProvider(
            apiKey: Environment.GetEnvironmentVariable("GEMINI_API_KEY")!)
        .WithClaudeProvider(
            apiKey: Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")!)
        .WithChatService()
);
```

## Chat Completions

### Simple Message

```csharp
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] string message)
    {
        try
        {
            var response = await _chatService.SendAsync(message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat error");
            return BadRequest(ex.Message);
        }
    }
}
```

### Streaming Response

```csharp
[HttpGet("stream")]
public async IAsyncEnumerable<string> StreamResponse(
    string query,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    await foreach (var chunk in _chatService.StreamAsync(query, cancellationToken: cancellationToken))
    {
        yield return chunk;
    }
}
```

### System Prompt with History

```csharp
public async Task<string> ChatWithHistory(
    string userMessage,
    List<ChatMessage> history)
{
    var messages = new List<ChatMessage>
    {
        ChatMessage.System("You are a helpful assistant specializing in technical questions."),
        ...history,
        ChatMessage.User(userMessage)
    };

    var response = await _chatService.SendAsync(messages);
    return response.Content;
}
```

## RAG Workflow

### Setup RAG Service

```csharp
services.AddScoped<RAGService>(sp =>
{
    var embeddingService = new EmbeddingService(
        sp.GetRequiredService<IEmbeddingProvider>(),
        new InMemoryVectorStore());
    
    var chatService = sp.GetRequiredService<IChatService>();
    
    return new RAGService(embeddingService, chatService);
});
```

### Index Documents

```csharp
public class DocumentIndexingService
{
    private readonly RAGService _ragService;

    public async Task IndexDocumentsAsync(List<string> documents)
    {
        for (int i = 0; i < documents.Count; i++)
        {
            await _ragService.IndexDocumentAsync($"doc-{i}", documents[i]);
        }
    }

    public async Task<(string answer, List<RetrievalResult> sources)> QueryAsync(
        string query,
        int topK = 5)
    {
        return await _ragService.AugmentAndGenerateAsync(query, topK);
    }
}
```

### Advanced RAG with Custom Ranking

```csharp
public async Task<List<RetrievalResult>> AdvancedRetrievalAsync(
    string query,
    RAGService ragService)
{
    // Use BM25 ranking
    var bm25Results = await ragService.RetrieveWithBM25Async(query, topK: 10);
    
    // Use Maximum Marginal Relevance for diversity
    var mmrResults = await ragService.RetrieveWithMMRAsync(
        query,
        topK: 5,
        lambda: 0.6); // 60% relevance, 40% diversity
    
    // Use Reciprocal Rank Fusion to combine multiple rankers
    var fusedResults = ReciprocalRankFusion.Fuse(
        new[] { bm25Results, mmrResults });
    
    return fusedResults.Take(5).ToList();
}
```

## Memory Management

### Conversation Memory

```csharp
public class ConversationalBot
{
    private readonly ConversationMemory _memory;
    private readonly IChatService _chatService;

    public ConversationalBot(IChatService chatService)
    {
        _chatService = chatService;
        _memory = new ConversationMemory(maxMessages: 50);
    }

    public async Task<string> ChatAsync(string userInput)
    {
        _memory.AddMessage(ChatMessage.User(userInput));

        var messages = _memory.GetLastMessages(10);
        var response = await _chatService.SendAsync(messages);

        _memory.AddMessage(ChatMessage.Assistant(response.Content));

        return response.Content;
    }
}
```

### Semantic Memory with Embedding Service

```csharp
public class KnowledgeBase
{
    private readonly EmbeddingService _embeddingService;
    private readonly SemanticMemory _semanticMemory;

    public async Task<string> RecallRelevantKnowledge(string query)
    {
        var recalled = await _semanticMemory.RecallAsync(query, topK: 3);
        return string.Join("\n", recalled.Select(r => r.Text));
    }

    public async Task LearnFact(string fact)
    {
        await _semanticMemory.SaveAsync(fact);
    }
}
```

## Agents & Tools

### Define Custom Tool

```csharp
public class Calculator : ITool
{
    public string Name => "calculator";
    public string Description => "Performs mathematical calculations";

    public async Task<string> ExecuteAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        // Parse and execute calculation
        var result = EvaluateMath(input);
        return result.ToString();
    }

    private decimal EvaluateMath(string expression)
    {
        // Implementation
        return 0;
    }
}

public class WebSearch : ITool
{
    public string Name => "web_search";
    public string Description => "Searches the web for current information";

    public async Task<string> ExecuteAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        // Implementation using HTTP client
        return await SearchAsync(query);
    }

    private async Task<string> SearchAsync(string query)
    {
        // Implementation
        return string.Empty;
    }
}
```

### Create and Use Agent

```csharp
public class ResearchAgent
{
    private readonly Agent _agent;
    private readonly IToolRegistry _toolRegistry;

    public ResearchAgent(
        IChatService chatService,
        IToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry;
        
        _toolRegistry.RegisterTool(new Calculator());
        _toolRegistry.RegisterTool(new WebSearch());

        _agent = new Agent("research", chatService, toolRegistry);
    }

    public async Task<string> ResearchAsync(string question)
    {
        var result = await _agent.ExecuteAsync(question);
        return result;
    }
}
```

## Embeddings & Search

### Vector Storage and Search

```csharp
public class DocumentSearch
{
    private readonly EmbeddingService _embeddingService;

    public async Task IndexAsync(List<string> documents)
    {
        foreach (var doc in documents)
        {
            await _embeddingService.EmbedAsync(doc);
        }
    }

    public async Task<List<RetrievalResult>> SearchAsync(
        string query,
        int topK = 5)
    {
        var results = await _embeddingService.SearchAsync(query, topK);
        return results;
    }

    public async Task<float> CalculateSimilarityAsync(
        string text1,
        string text2)
    {
        var emb1 = await _embeddingService.EmbedAsync(text1);
        var emb2 = await _embeddingService.EmbedAsync(text2);
        
        return Embedding.CosineSimilarity(emb1.Vector, emb2.Vector);
    }
}
```

## Error Handling

### Retry Logic with Fallback

```csharp
public class ResilientChatService
{
    private readonly ILLMProvider[] _providers;
    private readonly ILogger<ResilientChatService> _logger;

    public async Task<ChatResponse> SendWithRetryAsync(
        List<ChatMessage> messages)
    {
        return await AIErrorHandling.WithFallbackAsync(
            _providers.Select<ILLMProvider, Func<CancellationToken, Task<ChatResponse>>>(
                provider => ct => provider.CompleteAsync(messages, null, ct)
            ).ToList(),
            _logger
        );
    }
}
```

### Safe Operations with AIResult

```csharp
public async Task<AIResult<string>> SafeChatAsync(string message)
{
    return await AIErrorHandling.TrySafeAsync(
        async ct => await _chatService.SendAsync(message, cancellationToken: ct),
        _logger
    );
}

// Usage
var result = await SafeChatAsync("Hello");
var response = result.Match(
    onSuccess: r => $"Response: {r}",
    onFailure: err => $"Error: {err.Message}"
);
```

## Model Selection

### Context-Aware Routing

```csharp
public class SmartChatService
{
    private readonly ModelRouter _router;
    private readonly ContextAwareRouter _contextRouter;

    public SmartChatService(ModelRouter router)
    {
        _router = router;
        _contextRouter = new ContextAwareRouter(router);
    }

    public async Task<string> SendAsync(
        string message,
        bool urgent = false,
        bool costSensitive = false)
    {
        var context = new RequestContext
        {
            Priority = urgent ? RequestPriority.Speed :
                      costSensitive ? RequestPriority.Cost :
                      RequestPriority.Accuracy,
            EstimatedTokens = AIPerformance.EstimateTokenCount(message)
        };

        var provider = _contextRouter.SelectForContext(context);
        var response = await provider.CompleteAsync(
            new[] { ChatMessage.User(message) }
        );

        _router.RecordSuccess(provider.Name, TimeSpan.FromMilliseconds(100), response.TokensUsed);

        return response.Content;
    }
}
```

## Structured Output

### Classification with Validation

```csharp
public class TextClassifier
{
    private readonly IChatService _chatService;
    private readonly StructuredOutputHandler _structuredHandler;

    public async Task<StructuredOutputTypes.Classification> ClassifyAsync(
        string text,
        List<string> categories)
    {
        var prompt = $@"Classify this text into one category: {string.Join(", ", categories)}

Text: {text}

Respond with ONLY valid JSON.";

        var response = await _chatService.SendAsync(prompt);
        
        try
        {
            return response.ExtractStructured<StructuredOutputTypes.Classification>(_structuredHandler);
        }
        catch
        {
            return new StructuredOutputTypes.Classification
            {
                Label = categories.First(),
                Confidence = 0f
            };
        }
    }
}
```

### Entity Extraction

```csharp
public class EntityExtractor
{
    private readonly IChatService _chatService;
    private readonly StructuredOutputHandler _handler;

    public async Task<StructuredOutputTypes.EntityExtractionResult> ExtractAsync(
        string text)
    {
        var prompt = _handler.GetStructuredOutputInstruction<StructuredOutputTypes.EntityExtractionResult>(
            $"Extract named entities from this text: {text}"
        );

        var response = await _chatService.SendAsync(prompt);
        return response.ExtractStructured<StructuredOutputTypes.EntityExtractionResult>(_handler);
    }
}
```

## Complete Application Example

### Multi-Feature Chat Application

```csharp
[ApiController]
[Route("api/chat")]
public class AdvancedChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly RAGService _ragService;
    private readonly ConversationMemory _memory;
    private readonly ModelRouter _router;
    private readonly ILogger<AdvancedChatController> _logger;

    public AdvancedChatController(
        IChatService chatService,
        RAGService ragService,
        ModelRouter router,
        ILogger<AdvancedChatController> logger)
    {
        _chatService = chatService;
        _ragService = ragService;
        _memory = new ConversationMemory(maxMessages: 50);
        _router = router;
        _logger = logger;
    }

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] ChatRequest request)
    {
        return await AIPerformance.MeasureAsync(
            "chat_query",
            async ct =>
            {
                // Add to memory
                _memory.AddMessage(ChatMessage.User(request.Query));

                // Get context from RAG if applicable
                string? context = null;
                if (request.UseRAG)
                {
                    var (answer, sources) = await _ragService.AugmentAndGenerateAsync(
                        request.Query,
                        topK: 3);
                    context = string.Join("\n", sources.Select(s => s.Text));
                }

                // Build messages
                var messages = new List<ChatMessage>
                {
                    ChatMessage.System("You are a helpful assistant."),
                };

                if (!string.IsNullOrEmpty(context))
                    messages.Add(ChatMessage.System($"Context: {context}"));

                messages.AddRange(_memory.GetLastMessages(10));

                // Send with appropriate provider
                var response = await _chatService.SendAsync(messages, cancellationToken: ct);

                _memory.AddMessage(ChatMessage.Assistant(response.Content));

                return Ok(new ChatResponse
                {
                    Content = response.Content,
                    Tokens = response.TokensUsed,
                    Provider = response.Model
                });
            },
            _logger
        );
    }
}

public class ChatRequest
{
    public string Query { get; set; } = string.Empty;
    public bool UseRAG { get; set; }
    public List<string>? Categories { get; set; }
}
```

## Best Practices

### 1. Always use CancellationToken
```csharp
public async Task<T> MyOperationAsync(CancellationToken cancellationToken = default)
{
    return await _service.DoSomethingAsync(cancellationToken);
}
```

### 2. Log Important Operations
```csharp
_logger.LogInformation("Starting RAG query for: {Query}", query);
var result = await _ragService.AugmentAndGenerateAsync(query);
_logger.LogInformation("RAG completed with {SourceCount} sources", result.Sources.Count);
```

### 3. Handle Errors Gracefully
```csharp
var result = await AIErrorHandling.TrySafeAsync(
    async ct => await _chatService.SendAsync(message, cancellationToken: ct),
    _logger
);
```

### 4. Monitor Performance
```csharp
var estimatedCost = AIPerformance.EstimateCost(prompt, response);
_logger.LogDebug("Estimated cost: ${Cost}", estimatedCost);
```

### 5. Validate Responses
```csharp
if (!AIValidation.ValidateResponse(response, minLength: 50))
{
    _logger.LogWarning("Response validation failed");
}
```

## Configuration Best Practices

### Use Environment Variables
```csharp
var openaiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("Missing OPENAI_API_KEY");
```

### Support Multiple Configurations
```csharp
var aiConfig = configuration.GetSection("AI");
if (aiConfig["Provider"] == "Azure")
{
    services.AddAzureOpenAILLMProvider(...);
}
else
{
    services.AddOpenAILLMProvider(...);
}
```

## Performance Tips

1. **Cache Embeddings**: Store and reuse embeddings for frequently used texts
2. **Batch Operations**: Use batch endpoints when available
3. **Choose Appropriate Models**: Use faster models for simple tasks
4. **Monitor Token Usage**: Track tokens to manage costs
5. **Use Streaming**: For long responses, use streaming to improve UX

## Troubleshooting

| Issue | Solution |
|-------|----------|
| API Key Not Found | Check environment variables, use `.FromEnvironment()` |
| Slow Responses | Check model choice, consider streaming |
| High Costs | Monitor token usage, use cost-optimal routing |
| Memory Issues | Implement response pagination, clean up old messages |
| Timeout Errors | Increase timeout, use appropriate models |

