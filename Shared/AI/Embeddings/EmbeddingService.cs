namespace Shared.AI.Embeddings;

using Microsoft.Extensions.Logging;
using Shared.AI.Abstractions;

/// <summary>
/// Default in-memory vector store implementation.
/// Suitable for small to medium-sized datasets.
/// Thread-safe with concurrent access support.
/// </summary>
public class InMemoryVectorStore : IVectorStore
{
    private readonly Dictionary<string, VectorStoreItem> _items = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger? _logger;
    private bool _disposed;

    public InMemoryVectorStore(ILogger? logger = null)
    {
        _logger = logger;
    }

    public async Task<string> StoreAsync(
        string text,
        Embedding embedding,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var id = Guid.NewGuid().ToString();
        var item = new VectorStoreItem(id, text, embedding, metadata);

        _lock.EnterWriteLock();
        try
        {
            _items[id] = item;
            _logger?.LogDebug("Stored vector with ID: {Id}", id);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return await Task.FromResult(id);
    }

    public async Task<IReadOnlyList<string>> StoreBatchAsync(
        IReadOnlyList<(string text, Embedding embedding, Dictionary<string, object>? metadata)> items,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var ids = new List<string>();

        _lock.EnterWriteLock();
        try
        {
            foreach (var (text, embedding, metadata) in items)
            {
                var id = Guid.NewGuid().ToString();
                _items[id] = new VectorStoreItem(id, text, embedding, metadata);
                ids.Add(id);
            }

            _logger?.LogDebug("Stored {Count} vectors", items.Count);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return await Task.FromResult(ids.AsReadOnly());
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        Embedding query,
        int topK = 10,
        double? similarityThreshold = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _lock.EnterReadLock();
        try
        {
            var results = _items.Values
                .Select(item => new
                {
                    Item = item,
                    Similarity = query.CosineSimilarity(item.Embedding)
                })
                .Where(x => similarityThreshold == null || x.Similarity >= similarityThreshold)
                .OrderByDescending(x => x.Similarity)
                .Take(topK)
                .Select(x => new VectorSearchResult(
                    x.Item.Id,
                    x.Item.Text,
                    x.Similarity,
                    x.Item.Metadata))
                .ToList();

            _logger?.LogDebug("Found {Count} similar vectors (top {TopK})", results.Count, topK);

            return await Task.FromResult(results.AsReadOnly());
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _lock.EnterWriteLock();
        try
        {
            var removed = _items.Remove(id);
            if (removed)
                _logger?.LogDebug("Deleted vector with ID: {Id}", id);
            return removed;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task<VectorStoreItem?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _lock.EnterReadLock();
        try
        {
            _items.TryGetValue(id, out var item);
            return await Task.FromResult(item);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _lock.EnterReadLock();
        try
        {
            return await Task.FromResult(_items.Count);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _lock.EnterWriteLock();
        try
        {
            _items.Clear();
            _logger?.LogDebug("Cleared all vectors");
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        await Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _lock?.Dispose();
            _disposed = true;
        }

        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(InMemoryVectorStore));
    }
}

/// <summary>
/// Service for managing embeddings.
/// Handles generation, storage, and retrieval.
/// </summary>
public class EmbeddingService
{
    private readonly IEmbeddingProvider _provider;
    private readonly IVectorStore? _store;
    private readonly ILogger? _logger;

    public EmbeddingService(
        IEmbeddingProvider provider,
        IVectorStore? store = null,
        ILogger? logger = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Embeds and optionally stores text.
    /// </summary>
    public async Task<Embedding> EmbedAsync(
        string text,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Embedding text (length: {Length})", text.Length);

        var embedding = await _provider.EmbedAsync(text, cancellationToken);

        if (_store != null && metadata != null)
        {
            await _store.StoreAsync(text, embedding, metadata, cancellationToken);
        }

        return embedding;
    }

    /// <summary>
    /// Embeds multiple texts in batch.
    /// </summary>
    public async Task<IReadOnlyList<Embedding>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Embedding batch of {Count} texts", texts.Count);

        return await _provider.EmbedBatchAsync(texts, cancellationToken);
    }

    /// <summary>
    /// Searches for similar embeddings in the vector store.
    /// </summary>
    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string text,
        int topK = 10,
        double? similarityThreshold = null,
        CancellationToken cancellationToken = default)
    {
        if (_store == null)
            throw new InvalidOperationException("Vector store not configured");

        var queryEmbedding = await _provider.EmbedAsync(text, cancellationToken);
        return await _store.SearchAsync(queryEmbedding, topK, similarityThreshold, cancellationToken);
    }

    /// <summary>
    /// Gets embedding dimensions.
    /// </summary>
    public async Task<int> GetDimensionsAsync(CancellationToken cancellationToken = default)
    {
        return await _provider.GetDimensionsAsync(cancellationToken);
    }
}
