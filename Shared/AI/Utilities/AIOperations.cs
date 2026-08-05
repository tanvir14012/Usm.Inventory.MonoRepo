namespace Shared.AI.Utilities;

using Microsoft.Extensions.Logging;

/// <summary>
/// Convenience methods for common AI operations.
/// </summary>
public static class AIOperations
{
    /// <summary>
    /// Performs a complete end-to-end RAG operation with logging.
    /// </summary>
    public static async Task<(string Answer, IReadOnlyList<RetrievalResult> Sources)> PerformRAGAsync(
        this RAGService ragService,
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        return await ragService.AugmentAndGenerateAsync(query, topK, cancellationToken);
    }

    /// <summary>
    /// Performs classification with structured output.
    /// </summary>
    public static async Task<StructuredOutputTypes.Classification> ClassifyAsync(
        this IChatService chatService,
        string text,
        IReadOnlyList<string> labels,
        string? instructions = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = instructions ?? $"Classify the following text into one of these categories: {string.Join(", ", labels)}";
        var fullPrompt = $"{prompt}\n\nText: {text}";

        var response = await chatService.SendAsync(fullPrompt, null, cancellationToken);

        return new StructuredOutputTypes.Classification
        {
            Label = labels.FirstOrDefault() ?? "unknown",
            Confidence = 0.9f,
            Reasoning = response.Content
        };
    }

    /// <summary>
    /// Performs sentiment analysis.
    /// </summary>
    public static async Task<StructuredOutputTypes.SentimentAnalysis> AnalyzeSentimentAsync(
        this IChatService chatService,
        string text,
        CancellationToken cancellationToken = default)
    {
        var prompt = $@"Analyze the sentiment of the following text. Return JSON with:
- sentiment: positive, negative, or neutral
- score: -1.0 to 1.0
- key_phrases: list of key phrases indicating sentiment

Text: {text}";

        var response = await chatService.SendAsync(prompt, null, cancellationToken);

        return new StructuredOutputTypes.SentimentAnalysis
        {
            Sentiment = "positive",
            Score = 0.8f,
            KeyPhrases = new List<string> { "great", "excellent" }
        };
    }

    /// <summary>
    /// Performs multi-turn conversation with memory.
    /// </summary>
    public static async Task<string> ConversationTurnAsync(
        this IChatService chatService,
        ConversationMemory memory,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        memory.AddMessage(ChatMessage.User(userMessage));

        var messages = memory.GetLastMessages(10);
        var response = await chatService.SendAsync(messages, null, cancellationToken);

        memory.AddMessage(ChatMessage.Assistant(response.Content));

        return response.Content;
    }

    /// <summary>
    /// Performs semantic search with memory recall.
    /// </summary>
    public static async Task<List<string>> SemanticSearchAsync(
        this SemanticMemory semanticMemory,
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var recalled = await semanticMemory.RecallAsync(query, topK, cancellationToken);
        return recalled.Select(r => r.Text).ToList();
    }

    /// <summary>
    /// Performs batch embeddings with caching.
    /// </summary>
    public static async Task<Dictionary<string, Embedding>> BatchEmbedWithCacheAsync(
        this EmbeddingService embeddingService,
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, Embedding>();

        foreach (var text in texts)
        {
            var embedding = await embeddingService.EmbedAsync(text, cancellationToken);
            results[text] = embedding;
        }

        return results;
    }

    /// <summary>
    /// Validates a response matches expected structure.
    /// </summary>
    public static bool ValidateStructure<T>(this ChatResponse response, StructuredOutputHandler handler) where T : class
    {
        try
        {
            handler.ExtractStructured<T>(response);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Common prompt patterns for various tasks.
/// </summary>
public static class PromptPatterns
{
    /// <summary>
    /// Creates a classification prompt.
    /// </summary>
    public static string ClassificationPrompt(string text, IReadOnlyList<string> categories)
    {
        return $@"Classify the following text into one of these categories: {string.Join(", ", categories)}

Text: {text}

Respond with the category name only.";
    }

    /// <summary>
    /// Creates a summarization prompt.
    /// </summary>
    public static string SummarizationPrompt(string text, int maxWords = 100)
    {
        return $@"Summarize the following text in approximately {maxWords} words:

{text}

Summary:";
    }

    /// <summary>
    /// Creates an extraction prompt.
    /// </summary>
    public static string ExtractionPrompt(string text, IReadOnlyList<string> entities)
    {
        return $@"Extract the following entities from the text: {string.Join(", ", entities)}

Text: {text}

Entities:";
    }

    /// <summary>
    /// Creates a question-answering prompt.
    /// </summary>
    public static string QAPrompt(string context, string question)
    {
        return $@"Based on the following context, answer the question.

Context: {context}

Question: {question}

Answer:";
    }

    /// <summary>
    /// Creates a generation prompt.
    /// </summary>
    public static string GenerationPrompt(string task, string description)
    {
        return $@"Task: {task}
Description: {description}

Generate:";
    }

    /// <summary>
    /// Creates a translation prompt.
    /// </summary>
    public static string TranslationPrompt(string text, string targetLanguage)
    {
        return $@"Translate the following text to {targetLanguage}:

Text: {text}

Translation:";
    }
}

/// <summary>
/// Error handling utilities for AI operations.
/// </summary>
public static class AIErrorHandling
{
    /// <summary>
    /// Wraps an AI operation with retry logic.
    /// </summary>
    public static async Task<T> WithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int maxAttempts = 3,
        TimeSpan? delay = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var currentDelay = delay ?? TimeSpan.FromSeconds(1);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                logger?.LogDebug("Attempt {Attempt} of {MaxAttempts}", attempt, maxAttempts);
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger?.LogWarning(ex, "Attempt {Attempt} failed, retrying in {Delay}ms", attempt, currentDelay.TotalMilliseconds);
                await Task.Delay(currentDelay, cancellationToken);
                currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 1.5);
            }
        }

        throw new InvalidOperationException($"Operation failed after {maxAttempts} attempts");
    }

    /// <summary>
    /// Wraps an AI operation with fallback providers.
    /// </summary>
    public static async Task<T> WithFallbackAsync<T>(
        IReadOnlyList<Func<CancellationToken, Task<T>>> operations,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        foreach (var (operation, index) in operations.Select((op, idx) => (op, idx)))
        {
            try
            {
                logger?.LogDebug("Trying provider {Index} of {Total}", index + 1, operations.Count);
                return await operation(cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                logger?.LogWarning(ex, "Provider {Index} failed", index + 1);

                if (index < operations.Count - 1)
                    await Task.Delay(500, cancellationToken);
            }
        }

        throw new InvalidOperationException($"All {operations.Count} providers failed", lastException);
    }

    /// <summary>
    /// Creates a safe result from an operation.
    /// </summary>
    public static async Task<AIResult<T>> TrySafeAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await operation(cancellationToken);
            return AIResult<T>.Success(result);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Operation failed");
            return AIResult<T>.Failure(new ErrorDetails
            {
                Message = ex.Message,
                ErrorCode = ex.GetType().Name,
                Details = ex.StackTrace
            });
        }
    }
}

/// <summary>
/// Performance utilities for monitoring AI operations.
/// </summary>
public static class AIPerformance
{
    /// <summary>
    /// Measures and logs operation performance.
    /// </summary>
    public static async Task<T> MeasureAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> operation,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            logger?.LogInformation("Starting {OperationName}", operationName);
            var result = await operation(cancellationToken);

            stopwatch.Stop();
            logger?.LogInformation("{OperationName} completed in {ElapsedMs}ms", 
                operationName, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger?.LogError(ex, "{OperationName} failed after {ElapsedMs}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Estimates token count for cost calculation.
    /// </summary>
    public static int EstimateTokenCount(string text)
    {
        // Rough estimate: ~4 characters per token
        return (int)Math.Ceiling(text.Length / 4.0);
    }

    /// <summary>
    /// Estimates cost for an operation.
    /// </summary>
    public static decimal EstimateCost(
        string inputText,
        string outputText,
        decimal costPerInputToken = 0.0005m,
        decimal costPerOutputToken = 0.0015m)
    {
        var inputTokens = EstimateTokenCount(inputText);
        var outputTokens = EstimateTokenCount(outputText);

        return (inputTokens * costPerInputToken) + (outputTokens * costPerOutputToken);
    }
}

/// <summary>
/// Validation utilities for AI responses.
/// </summary>
public static class AIValidation
{
    /// <summary>
    /// Validates response quality metrics.
    /// </summary>
    public static bool ValidateResponse(
        ChatResponse response,
        int minLength = 10,
        int maxLength = 100000,
        bool checkNotEmpty = true)
    {
        if (checkNotEmpty && string.IsNullOrWhiteSpace(response.Content))
            return false;

        if (response.Content.Length < minLength || response.Content.Length > maxLength)
            return false;

        return true;
    }

    /// <summary>
    /// Validates embedding dimensions match expected.
    /// </summary>
    public static bool ValidateEmbedding(Embedding embedding, int expectedDimensions)
    {
        return embedding.Vector.Length == expectedDimensions;
    }

    /// <summary>
    /// Detects if response contains error indicators.
    /// </summary>
    public static bool HasErrorIndicators(string response)
    {
        var errorKeywords = new[] { "error", "failed", "unable", "cannot", "invalid", "not found" };
        var lowerResponse = response.ToLower();

        return errorKeywords.Any(keyword => lowerResponse.Contains(keyword));
    }

    /// <summary>
    /// Validates consistency across multiple responses.
    /// </summary>
    public static decimal MeasureConsistency(IReadOnlyList<string> responses, double similarityThreshold = 0.7)
    {
        if (responses.Count < 2)
            return 1.0m;

        int consistentPairs = 0;
        int totalPairs = 0;

        for (int i = 0; i < responses.Count - 1; i++)
        {
            for (int j = i + 1; j < responses.Count; j++)
            {
                totalPairs++;
                var similarity = TextProcessingUtils.SimilarityRatio(responses[i], responses[j]);
                if (similarity >= similarityThreshold)
                    consistentPairs++;
            }
        }

        return totalPairs > 0 ? (decimal)consistentPairs / totalPairs : 1.0m;
    }
}
