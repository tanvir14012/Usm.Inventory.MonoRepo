namespace Shared.AI.Memory;

using System.Collections.Generic;
using Shared.AI.Abstractions;
using Shared.AI.Embeddings;

/// <summary>
/// Manages conversation history with optional summarization and windowing.
/// </summary>
public class ConversationMemory
{
    private readonly List<ChatMessage> _messages = new();
    private readonly int _maxMessages;
    private readonly ReaderWriterLockSlim _lock = new();

    public ConversationMemory(int maxMessages = 100)
    {
        _maxMessages = maxMessages;
    }

    /// <summary>
    /// Adds a message to the conversation.
    /// </summary>
    public void AddMessage(ChatMessage message)
    {
        _lock.EnterWriteLock();
        try
        {
            _messages.Add(message);

            // Remove oldest messages if exceeding limit
            while (_messages.Count > _maxMessages)
            {
                _messages.RemoveAt(0);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Adds multiple messages.
    /// </summary>
    public void AddMessages(IEnumerable<ChatMessage> messages)
    {
        foreach (var msg in messages)
            AddMessage(msg);
    }

    /// <summary>
    /// Gets all messages in the conversation.
    /// </summary>
    public IReadOnlyList<ChatMessage> GetMessages()
    {
        _lock.EnterReadLock();
        try
        {
            return _messages.ToList().AsReadOnly();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the last N messages.
    /// </summary>
    public IReadOnlyList<ChatMessage> GetLastMessages(int count)
    {
        _lock.EnterReadLock();
        try
        {
            return _messages.Skip(Math.Max(0, _messages.Count - count)).ToList().AsReadOnly();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets messages since a specific index.
    /// </summary>
    public IReadOnlyList<ChatMessage> GetMessagesSince(int sinceIndex)
    {
        _lock.EnterReadLock();
        try
        {
            if (sinceIndex < 0 || sinceIndex >= _messages.Count)
                return new List<ChatMessage>().AsReadOnly();

            return _messages.Skip(sinceIndex).ToList().AsReadOnly();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Clears all messages.
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _messages.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets the number of messages.
    /// </summary>
    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _messages.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}

/// <summary>
/// Semantic memory using vector embeddings.
/// Stores and retrieves information based on semantic similarity.
/// </summary>
public class SemanticMemory
{
    private readonly IVectorStore _vectorStore;
    private readonly Embeddings.EmbeddingService _embeddingService;

    public SemanticMemory(IVectorStore vectorStore, Embeddings.EmbeddingService embeddingService)
    {
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
    }

    /// <summary>
    /// Stores a fact or piece of knowledge.
    /// </summary>
    public async Task<string> SaveAsync(
        string content,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingService.EmbedAsync(content, metadata, cancellationToken);
        return await _vectorStore.StoreAsync(content, embedding, metadata, cancellationToken);
    }

    /// <summary>
    /// Searches for similar memories.
    /// </summary>
    public async Task<IReadOnlyList<VectorSearchResult>> RecallAsync(
        string query,
        int topK = 5,
        double? similarityThreshold = null,
        CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await _embeddingService._provider.EmbedAsync(query, cancellationToken);
        return await _vectorStore.SearchAsync(queryEmbedding, topK, similarityThreshold, cancellationToken);
    }

    /// <summary>
    /// Deletes a memory by ID.
    /// </summary>
    public async Task<bool> ForgetAsync(string memoryId, CancellationToken cancellationToken = default)
    {
        return await _vectorStore.DeleteAsync(memoryId, cancellationToken);
    }

    /// <summary>
    /// Gets total number of memories.
    /// </summary>
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _vectorStore.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Clears all memories.
    /// </summary>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _vectorStore.ClearAsync(cancellationToken);
    }
}

/// <summary>
/// Memory windowing strategy - keeps only recent messages.
/// </summary>
public class WindowingStrategy
{
    private readonly int _windowSize;

    public WindowingStrategy(int windowSize = 10)
    {
        _windowSize = windowSize;
    }

    /// <summary>
    /// Applies windowing to messages.
    /// </summary>
    public IReadOnlyList<ChatMessage> ApplyWindow(IReadOnlyList<ChatMessage> messages)
    {
        if (messages.Count <= _windowSize)
            return messages;

        return messages.Skip(messages.Count - _windowSize).ToList().AsReadOnly();
    }
}

/// <summary>
/// Memory summarization strategy - summarizes old messages.
/// </summary>
public class SummarizationStrategy
{
    private readonly IChatService _chatService;
    private readonly int _summarizeAfter;

    public SummarizationStrategy(IChatService chatService, int summarizeAfter = 20)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _summarizeAfter = summarizeAfter;
    }

    /// <summary>
    /// Applies summarization strategy to messages.
    /// </summary>
    public async Task<IReadOnlyList<ChatMessage>> ApplySummarizationAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (messages.Count < _summarizeAfter)
            return messages;

        var toSummarize = messages.Take(messages.Count - 10).ToList();
        var toKeep = messages.Skip(messages.Count - 10).ToList();

        var summaryPrompt = $"Summarize the following conversation:\n\n" +
            string.Join("\n", toSummarize.Select(m => $"{m.Role}: {m.Content}"));

        var summaryResponse = await _chatService.SendAsync(summaryPrompt, cancellationToken: cancellationToken);

        var result = new List<ChatMessage>
        {
            ChatMessage.System($"Summary of previous conversation: {summaryResponse.Content}")
        };
        result.AddRange(toKeep);

        return result.AsReadOnly();
    }
}
