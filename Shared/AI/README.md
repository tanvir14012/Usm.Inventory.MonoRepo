# Shared.AI Framework

A production-ready, provider-agnostic AI framework for enterprise .NET applications.

## Features

### Abstractions
- **ILLMProvider**: Unified interface for LLM providers (OpenAI, Azure, Ollama, etc.)
- **IEmbeddingProvider**: Vector generation and embeddings
- **IChatService**: High-level chat service with streaming support
- **IToolRegistry**: Function/tool calling infrastructure
- **IVectorStore**: Semantic search and vector storage
- **AIResult<T>**: Functional error handling with monadic interface

### Providers
- **OpenAI**: GPT-4, GPT-3.5, embeddings
- **Azure OpenAI**: Azure-hosted models with key-based auth
- **Ollama**: Local LLM support (Llama, Mistral, etc.)
- **Extensible**: Easy to add custom providers

### Core Features
- **Chat Completion**: With streaming and batching
- **Embeddings**: Vector generation and similarity search
- **RAG** (Retrieval-Augmented Generation): Document indexing and retrieval
- **Memory Management**: Conversation and semantic memory
- **Agents**: Agent loop with tool execution
- **Prompt Templates**: Variable substitution and caching

### Advanced Capabilities
- **Retry & Fallback**: Exponential backoff, fallback strategies
- **Token Estimation**: Token count approximation
- **Text Chunking**: Sliding window and recursive strategies
- **Vector Search**: Cosine similarity, BM25, MMR ranking
- **ML.NET**: Training pipelines and predictions
- **Python Integration**: Execute Python scripts and libraries

## Quick Start

### Installation

```csharp
// In Program.cs
services.AddAIFramework(builder => 
    builder
        .WithOpenAIProvider(c => c
            .WithModel("gpt-4")
            .FromEnvironment("OPENAI"))
        .WithChatService()
);
```

### Basic Usage

```csharp
var chatService = serviceProvider.GetRequiredService<IChatService>();

// Simple message
var response = await chatService.SendAsync("Hello, world!");
Console.WriteLine(response.Content);

// Conversation
var messages = new[]
{
    ChatMessage.System("You are a helpful assistant."),
    ChatMessage.User("What is 2+2?")
};

var response = await chatService.SendAsync(messages);
Console.WriteLine(response.Content);

// Streaming
await foreach (var chunk in chatService.StreamAsync("Tell me a story"))
{
    Console.Write(chunk);
}
```

### RAG Example

```csharp
var embeddingService = new EmbeddingService(embeddingProvider);
var vectorStore = new InMemoryVectorStore();
var ragService = new RAGService(embeddingService, chatService);

// Index a document
await ragService.IndexDocumentAsync("doc1", "The quick brown fox jumps over the lazy dog");

// Query with context
var (response, sources) = await ragService.AugmentAndGenerateAsync("What jumps?");
Console.WriteLine($"Answer: {response}");
foreach (var source in sources)
{
    Console.WriteLine($"Source: {source.Text} (relevance: {source.Similarity:P})");
}
```

### Agent with Tools

```csharp
var toolRegistry = serviceProvider.GetRequiredService<IToolRegistry>();

// Register a tool
toolRegistry.RegisterTool(new Calculator());

// Create and execute agent
var agent = new Agent("math-agent", chatService, toolRegistry);
var result = await agent.ExecuteAsync("What is 25 * 4?");
Console.WriteLine(result);
```

### Embeddings & Vector Search

```csharp
var embeddingService = new EmbeddingService(embeddingProvider, vectorStore);

// Generate and store embeddings
var embedding = await embeddingService.EmbedAsync("Hello world");

// Search
var results = await embeddingService.SearchAsync("greeting", topK: 5);
foreach (var result in results)
{
    Console.WriteLine($"{result.Text} (similarity: {result.Similarity:P})");
}
```

## Providers Configuration

### OpenAI

```csharp
.AddOpenAILLMProvider(c => c
    .WithModel("gpt-4")
    .WithApiKey(apiKey)
    .WithTemperature(0.7)
    .WithMaxTokens(2000))
.AddOpenAIEmbeddingProvider(c => c
    .WithModel("text-embedding-3-small")
    .WithApiKey(apiKey))
```

### Azure OpenAI

```csharp
.WithProvider("AzureOpenAI")
.WithModel("gpt-4-deployment")  // Deployment name
.WithEndpoint("https://your-resource.openai.azure.com")
.WithApiKey(azureApiKey)
```

### Ollama (Local)

```csharp
.AddOllamaLLMProvider(c => c
    .WithModel("llama2")
    .WithEndpoint("http://localhost:11434"))
.AddOllamaEmbeddingProvider(c => c
    .WithModel("nomic-embed-text"))
```

## Memory Management

### Conversation Memory

```csharp
var memory = new ConversationMemory(maxMessages: 50);

memory.AddMessage(ChatMessage.User("Hello"));
memory.AddMessage(ChatMessage.Assistant("Hi there!"));

var messages = memory.GetLastMessages(10);  // Last 10 messages
```

### Semantic Memory

```csharp
var semanticMemory = new SemanticMemory(vectorStore, embeddingService);

// Save facts
await semanticMemory.SaveAsync("The capital of France is Paris");

// Recall related information
var recalled = await semanticMemory.RecallAsync("France capital");
```

### Memory Strategies

```csharp
// Windowing: Keep only recent messages
var windowing = new WindowingStrategy(windowSize: 10);
var windowed = windowing.ApplyWindow(messages);

// Summarization: Summarize old messages
var summarization = new SummarizationStrategy(chatService);
var summarized = await summarization.ApplySummarizationAsync(messages);
```

## Prompting & Templates

```csharp
// Using template
var template = new PromptTemplate("What is {{topic}}? Explain in {{style}}.");
var formatted = template.Format(new Dictionary<string, object>
{
    ["topic"] = "machine learning",
    ["style"] = "simple terms"
});

// Using builder
var tpl = PromptTemplate.Builder()
    .AddText("Classify the following sentiment:")
    .AddSection("Task", "Determine if positive, negative, or neutral")
    .AddExample("Great product!", "positive")
    .AddVariable("text")
    .Build();

// Caching
var cache = new PromptCache();
var result = cache.GetOrGenerate(
    "classification",
    template,
    new Dictionary<string, object> { ["text"] = "Awesome!" }
);
```

## Error Handling

```csharp
// Using AIResult for functional error handling
var result = await chatService.SendAsync("Hello");

// Pattern matching
var message = result.Match(
    onSuccess: response => response.Content,
    onFailure: error => $"Error: {error.Message}"
);

// Monadic operations
var processed = result
    .Map(r => r.Content.ToUpper())
    .Bind(content => /* next operation */);
```

## Retry & Fallback

```csharp
var retryPolicy = new ExponentialBackoffPolicy(
    maxAttempts: 3,
    initialDelay: TimeSpan.FromSeconds(1),
    multiplier: 2.0);

var executor = new ResilientExecutor<string, string>(
    async (input, ct) => AIResult<string>.Success(input),
    retryPolicy);

executor.WithFallback(new CacheFallbackStrategy<string, string>(
    request => GetCachedResponse(request)));

var result = await executor.ExecuteAsync("query");
```

## Algorithms

### Similarity
```csharp
var similarity = SimilarityAlgorithms.CosineSimilarity(vec1, vec2);
var distance = SimilarityAlgorithms.EuclideanDistance(vec1, vec2);
var manhattan = SimilarityAlgorithms.ManhattanDistance(vec1, vec2);
```

### String Matching
```csharp
var distance = StringSimilarity.LevenshteinDistance("hello", "hallo");
var ratio = StringSimilarity.SimilarityRatio("hello", "hallo");
var jaro = StringSimilarity.JaroSimilarity("hello", "hallo");
```

### Ranking
```csharp
var bm25 = new BM25Ranker();
bm25.Train(documents);
var ranked = bm25.Rank("query", documents);

var rrf = ReciprocalRankFusion.Fuse(ranking1, ranking2, ranking3);
```

### Text Chunking
```csharp
// Sliding window with overlap
var chunks = TextChunking.SlidingWindowChunk(text, chunkSize: 512, overlap: 50);

// Recursive chunking
var recursive = TextChunking.RecursiveChunk(text, maxChunkSize: 1024,
    separators: new[] { "\n\n", "\n", " " });
```

## ML.NET Integration

```csharp
var mlService = new MLNetService();

// Train classifier
var pipeline = mlService.CreateTextClassificationPipeline<TextClassificationInput, TextClassificationOutput>();
var model = pipeline.Fit(trainingData);

// Predict
var engine = mlService.CreatePredictionEngine<TextClassificationInput, TextClassificationOutput>(model);
var prediction = engine.Predict(new TextClassificationInput { Text = "Great movie!" });

// Evaluate
var metrics = mlService.EvaluateClassificationModel(model, testData, "Label");
Console.WriteLine($"Accuracy: {metrics.Accuracy}");
```

## Python Integration

```csharp
var pythonManager = new PythonProcessManager();

// Execute script
var output = await pythonManager.ExecuteScriptAsync(@"
import json
result = sum([1, 2, 3, 4, 5])
print(json.dumps(result))
");

// Use transformers
var transformers = new TransformersWrapper(pythonManager);
var sentiment = await transformers.ClassifyTextAsync("This is amazing!");

// Use spaCy
var spacyWrapper = new spaCyWrapper(pythonManager);
var entities = await spacyWrapper.ExtractEntitiesAsync("John lives in New York");
```

## Performance Considerations

- **Embedding Caching**: Cache embeddings for frequently used texts
- **Prompt Caching**: Use PromptCache to avoid regenerating identical prompts
- **Batch Operations**: Use batch endpoints for embeddings
- **Vector Store Choice**: InMemoryVectorStore is fast for small datasets; use external DBs for large-scale
- **Token Estimation**: Use TokenEstimator to predict costs before API calls
- **Connection Pooling**: Reuse HttpClient instances

## Thread Safety

All components are designed to be thread-safe:
- `ConversationMemory`: Uses ReaderWriterLockSlim
- `InMemoryVectorStore`: Uses ReaderWriterLockSlim
- `ToolRegistry`: Thread-safe concurrent access
- Async-first design for scalability

## Testing

```csharp
// Mock provider for testing
var mockProvider = new MockLLMProvider();
var chatService = new ChatService(mockProvider);

var response = await chatService.SendAsync("test");
Assert.AreEqual("expected", response.Content);
```

## Contributing

Contributions welcome! Please follow SOLID principles and include XML documentation.

## License

Part of the Usm.Inventory.MonoRepo project.

## Roadmap

- [ ] Gemini provider
- [ ] Claude provider
- [ ] Semantic Kernel adapter
- [ ] Postgres vector store
- [ ] Redis caching layer
- [ ] Kubernetes scaling
- [ ] Telemetry and observability
- [ ] Structured output (JSON Schema)
- [ ] Fine-tuning support

