namespace Shared.AI.Abstractions;

/// <summary>
/// Represents a vector embedding of dimensions.
/// </summary>
public class Embedding
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Embedding"/> class.
    /// </summary>
    /// <param name="vector">The embedding vector.</param>
    /// <param name="model">The model used to generate the embedding.</param>
    /// <param name="dimensions">The dimensions of the vector.</param>
    public Embedding(ReadOnlyMemory<float> vector, string? model = null, int? dimensions = null)
    {
        Vector = vector;
        Model = model;
        Dimensions = dimensions ?? vector.Length;
    }

    /// <summary>Gets the embedding vector.</summary>
    public ReadOnlyMemory<float> Vector { get; }

    /// <summary>Gets the model used for the embedding.</summary>
    public string? Model { get; }

    /// <summary>Gets the dimensions of the vector.</summary>
    public int Dimensions { get; }

    /// <summary>
    /// Calculates cosine similarity with another embedding.
    /// </summary>
    public double CosineSimilarity(Embedding other)
    {
        var v1 = Vector.Span;
        var v2 = other.Vector.Span;

        if (v1.Length != v2.Length)
            throw new ArgumentException("Embedding dimensions must match");

        float dotProduct = 0;
        float magnitude1 = 0;
        float magnitude2 = 0;

        for (int i = 0; i < v1.Length; i++)
        {
            dotProduct += v1[i] * v2[i];
            magnitude1 += v1[i] * v1[i];
            magnitude2 += v2[i] * v2[i];
        }

        var denom = Math.Sqrt((double)magnitude1) * Math.Sqrt((double)magnitude2);
        if (denom == 0) return 0;

        return dotProduct / denom;
    }
}

/// <summary>
/// Configuration for an embedding provider.
/// </summary>
public interface IEmbeddingProviderConfig
{
    /// <summary>Gets the provider name.</summary>
    string ProviderName { get; }

    /// <summary>Gets the embedding model identifier.</summary>
    string Model { get; }

    /// <summary>Gets the API key.</summary>
    string? ApiKey { get; }

    /// <summary>Gets the endpoint URL.</summary>
    string? Endpoint { get; }

    /// <summary>Gets expected dimensions for embeddings.</summary>
    int? Dimensions { get; }
}

/// <summary>
/// Interface for embedding providers (vector generation).
/// Supports providers like OpenAI, Ollama, ONNX, etc.
/// </summary>
public interface IEmbeddingProvider : IAsyncDisposable
{
    /// <summary>Gets the configuration for this provider.</summary>
    IEmbeddingProviderConfig Config { get; }

    /// <summary>
    /// Generates an embedding for a single text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated embedding.</returns>
    Task<Embedding> EmbedAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for multiple texts.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of embeddings.</returns>
    Task<IReadOnlyList<Embedding>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the dimensions of embeddings from this provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The embedding dimensions.</returns>
    Task<int> GetDimensionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the provider connection and authentication.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection is successful.</returns>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}
