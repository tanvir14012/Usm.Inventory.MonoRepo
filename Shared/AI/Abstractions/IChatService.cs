namespace Shared.AI.Abstractions;

/// <summary>
/// Service for managing AI chat operations.
/// Provides high-level chat functionality on top of providers.
/// </summary>
public interface IChatService : IAsyncDisposable
{
    /// <summary>
    /// Sends a single message and gets a response.
    /// </summary>
    Task<ChatResponse> SendAsync(
        string message,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a conversation and gets a response.
    /// </summary>
    Task<ChatResponse> SendAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a response for a single message.
    /// </summary>
    IAsyncEnumerable<string> StreamAsync(
        string message,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a response for a conversation.
    /// </summary>
    IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for tool/function calling support.
/// Allows AI to invoke defined functions.
/// </summary>
public interface ITool
{
    /// <summary>Gets the tool name.</summary>
    string Name { get; }

    /// <summary>Gets the tool description.</summary>
    string Description { get; }

    /// <summary>Gets the tool parameters schema (JSON Schema).</summary>
    string ParametersSchema { get; }

    /// <summary>
    /// Executes the tool with given arguments.
    /// </summary>
    Task<string> ExecuteAsync(string arguments, CancellationToken cancellationToken = default);
}

/// <summary>
/// Registry for managing available tools.
/// </summary>
public interface IToolRegistry : IAsyncDisposable
{
    /// <summary>
    /// Registers a tool.
    /// </summary>
    void RegisterTool(ITool tool);

    /// <summary>
    /// Registers multiple tools.
    /// </summary>
    void RegisterTools(IEnumerable<ITool> tools);

    /// <summary>
    /// Removes a tool by name.
    /// </summary>
    bool RemoveTool(string toolName);

    /// <summary>
    /// Gets a tool by name.
    /// </summary>
    ITool? GetTool(string toolName);

    /// <summary>
    /// Gets all registered tools.
    /// </summary>
    IReadOnlyList<ITool> GetAllTools();

    /// <summary>
    /// Checks if a tool is registered.
    /// </summary>
    bool HasTool(string toolName);

    /// <summary>
    /// Executes a tool by name.
    /// </summary>
    Task<string> ExecuteToolAsync(string toolName, string arguments, CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration for vector store (database for embeddings).
/// </summary>
public interface IVectorStoreConfig
{
    /// <summary>Gets the store type name.</summary>
    string StoreType { get; }

    /// <summary>Gets the connection string.</summary>
    string? ConnectionString { get; }

    /// <summary>Gets the embedding dimensions.</summary>
    int? EmbeddingDimensions { get; }
}

/// <summary>
/// Interface for storing and retrieving embeddings (vector database).
/// </summary>
public interface IVectorStore : IAsyncDisposable
{
    /// <summary>
    /// Stores an embedding with metadata.
    /// </summary>
    Task<string> StoreAsync(
        string text,
        Embedding embedding,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores multiple embeddings.
    /// </summary>
    Task<IReadOnlyList<string>> StoreBatchAsync(
        IReadOnlyList<(string text, Embedding embedding, Dictionary<string, object>? metadata)> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for similar vectors.
    /// </summary>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        Embedding query,
        int topK = 10,
        double? similarityThreshold = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an item by ID.
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an item by ID.
    /// </summary>
    Task<VectorStoreItem?> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of items.
    /// </summary>
    Task<long> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all items.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a vector similarity search.
/// </summary>
public class VectorSearchResult
{
    public VectorSearchResult(string id, string text, double similarity, Dictionary<string, object>? metadata = null)
    {
        Id = id;
        Text = text;
        Similarity = similarity;
        Metadata = metadata ?? new();
    }

    /// <summary>Gets the item ID.</summary>
    public string Id { get; }

    /// <summary>Gets the original text.</summary>
    public string Text { get; }

    /// <summary>Gets the similarity score (0-1).</summary>
    public double Similarity { get; }

    /// <summary>Gets associated metadata.</summary>
    public Dictionary<string, object> Metadata { get; }
}

/// <summary>
/// Item stored in the vector store.
/// </summary>
public class VectorStoreItem
{
    public VectorStoreItem(string id, string text, Embedding embedding, Dictionary<string, object>? metadata = null)
    {
        Id = id;
        Text = text;
        Embedding = embedding;
        Metadata = metadata ?? new();
    }

    /// <summary>Gets the item ID.</summary>
    public string Id { get; }

    /// <summary>Gets the text content.</summary>
    public string Text { get; }

    /// <summary>Gets the embedding.</summary>
    public Embedding Embedding { get; }

    /// <summary>Gets associated metadata.</summary>
    public Dictionary<string, object> Metadata { get; }
}
