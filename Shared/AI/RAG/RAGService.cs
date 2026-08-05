namespace Shared.AI.RAG;

using Microsoft.Extensions.Logging;
using Shared.AI.Abstractions;
using Shared.AI.Embeddings;
using Shared.AI.Utilities;

/// <summary>
/// RAG (Retrieval-Augmented Generation) service.
/// Retrieves relevant documents and augments LLM prompts.
/// </summary>
public class RAGService
{
    private readonly EmbeddingService _embeddingService;
    private readonly IChatService _chatService;
    private readonly ILogger? _logger;

    public RAGService(
        EmbeddingService embeddingService,
        IChatService chatService,
        ILogger? logger = null)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _logger = logger;
    }

    /// <summary>
    /// Indexes a document by splitting and embedding it.
    /// </summary>
    public async Task<IReadOnlyList<string>> IndexDocumentAsync(
        string documentId,
        string content,
        int chunkSize = 512,
        int overlapSize = 50,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Indexing document: {DocumentId} (size: {Size})", documentId, content.Length);

        var chunks = TextChunking.SlidingWindowChunk(content, chunkSize, overlapSize).ToList();
        var chunkIds = new List<string>();

        foreach (var (chunk, index) in chunks.Select((c, i) => (c, i)))
        {
            var metadata = new Dictionary<string, object>
            {
                ["document_id"] = documentId,
                ["chunk_index"] = index,
                ["chunk_count"] = chunks.Count
            };

            var embedding = await _embeddingService.EmbedAsync(chunk, metadata, cancellationToken);
            var id = $"doc:{documentId}:{index}";
            chunkIds.Add(id);
        }

        _logger?.LogDebug("Indexed {Count} chunks from document {DocumentId}", chunks.Count, documentId);
        return chunkIds.AsReadOnly();
    }

    /// <summary>
    /// Retrieves relevant chunks for a query.
    /// </summary>
    public async Task<IReadOnlyList<VectorSearchResult>> RetrieveAsync(
        string query,
        int topK = 5,
        double? similarityThreshold = null,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Retrieving context for query: {Query}", query);

        var results = await _embeddingService.SearchAsync(
            query,
            topK,
            similarityThreshold,
            cancellationToken);

        _logger?.LogDebug("Retrieved {Count} relevant chunks", results.Count);
        return results;
    }

    /// <summary>
    /// Performs RAG by retrieving context and generating a response.
    /// </summary>
    public async Task<(string response, IReadOnlyList<VectorSearchResult> sources)> AugmentAndGenerateAsync(
        string query,
        string systemPrompt = "You are a helpful assistant.",
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var context = await RetrieveAsync(query, topK, cancellationToken: cancellationToken);

        var contextText = string.Join("\n\n", context.Select(c =>
            $"[Source: {c.Id} (Relevance: {c.Similarity:P})] {c.Text}"));

        var augmentedPrompt = string.IsNullOrEmpty(contextText)
            ? query
            : $"Context:\n{contextText}\n\nQuestion: {query}";

        var messages = new[]
        {
            ChatMessage.System(systemPrompt),
            ChatMessage.User(augmentedPrompt)
        };

        var response = await _chatService.SendAsync(messages, null, cancellationToken);

        return (response.Content, context);
    }

    /// <summary>
    /// Streams RAG response with context-aware generation.
    /// </summary>
    public async IAsyncEnumerable<string> StreamAugmentedResponseAsync(
        string query,
        string systemPrompt = "You are a helpful assistant.",
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var context = await RetrieveAsync(query, topK, cancellationToken: cancellationToken);

        var contextText = string.Join("\n\n", context.Select(c =>
            $"[Source: {c.Id}] {c.Text}"));

        var augmentedPrompt = string.IsNullOrEmpty(contextText)
            ? query
            : $"Context:\n{contextText}\n\nQuestion: {query}";

        var messages = new[]
        {
            ChatMessage.System(systemPrompt),
            ChatMessage.User(augmentedPrompt)
        };

        await foreach (var chunk in _chatService.StreamAsync(messages, cancellationToken: cancellationToken))
        {
            yield return chunk;
        }
    }
}

/// <summary>
/// BM25 ranking algorithm for information retrieval.
/// Better than simple cosine similarity for sparse retrieval.
/// </summary>
public class BM25Ranker
{
    private const double K1 = 1.5;
    private const double B = 0.75;
    private readonly Dictionary<string, int> _documentFrequency = new();
    private readonly Dictionary<string, double> _inversedDocumentFrequency = new();
    private double _avgDocumentLength = 0;
    private int _totalDocuments = 0;

    /// <summary>
    /// Trains the BM25 ranker on documents.
    /// </summary>
    public void Train(IEnumerable<string> documents)
    {
        var docs = documents.ToList();
        _totalDocuments = docs.Count;

        var documentTerms = new List<HashSet<string>>();

        foreach (var doc in docs)
        {
            var terms = Tokenize(doc);
            documentTerms.Add(new HashSet<string>(terms));
            _avgDocumentLength += terms.Count;

            foreach (var term in terms)
            {
                if (!_documentFrequency.ContainsKey(term))
                    _documentFrequency[term] = 0;
                _documentFrequency[term]++;
            }
        }

        _avgDocumentLength /= _totalDocuments;

        foreach (var term in _documentFrequency.Keys)
        {
            var df = _documentFrequency[term];
            _inversedDocumentFrequency[term] = Math.Log((_totalDocuments - df + 0.5) / (df + 0.5) + 1);
        }
    }

    /// <summary>
    /// Ranks documents by BM25 score.
    /// </summary>
    public IEnumerable<(string document, double score)> Rank(string query, IEnumerable<string> documents)
    {
        var queryTerms = Tokenize(query);
        var results = new List<(string, double)>();

        foreach (var doc in documents)
        {
            var docTerms = Tokenize(doc);
            var score = CalculateScore(queryTerms, docTerms);
            results.Add((doc, score));
        }

        return results.OrderByDescending(x => x.Item2);
    }

    private List<string> Tokenize(string text)
    {
        return text
            .ToLower()
            .Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    private double CalculateScore(List<string> queryTerms, List<string> docTerms)
    {
        double score = 0;
        var docLength = docTerms.Count;

        foreach (var term in queryTerms)
        {
            if (!_inversedDocumentFrequency.ContainsKey(term))
                continue;

            var termFrequency = docTerms.Count(t => t == term);
            var idf = _inversedDocumentFrequency[term];

            var numerator = termFrequency * (K1 + 1);
            var denominator = termFrequency + K1 * (1 - B + B * (docLength / _avgDocumentLength));

            score += idf * (numerator / denominator);
        }

        return score;
    }
}

/// <summary>
/// Reciprocal Rank Fusion (RRF) for combining multiple retrieval methods.
/// </summary>
public static class ReciprocalRankFusion
{
    /// <summary>
    /// Fuses multiple ranking results using RRF.
    /// </summary>
    public static IEnumerable<(string item, double score)> Fuse(
        params IEnumerable<string>[] rankings)
    {
        var itemScores = new Dictionary<string, double>();
        const double constant = 60;

        foreach (var ranking in rankings)
        {
            foreach (var (item, rank) in ranking.Select((item, rank) => (item, rank + 1)))
            {
                if (!itemScores.ContainsKey(item))
                    itemScores[item] = 0;
                itemScores[item] += 1 / (constant + rank);
            }
        }

        return itemScores
            .OrderByDescending(x => x.Value)
            .Select(x => (x.Key, x.Value));
    }
}

/// <summary>
/// Maximum Marginal Relevance (MMR) for diversity-aware retrieval.
/// </summary>
public static class MaximumMarginalRelevance
{
    /// <summary>
    /// Selects documents that maximize relevance while minimizing redundancy.
    /// </summary>
    public static List<T> Select<T>(
        IEnumerable<T> documents,
        Func<T, T, double> relevance,
        Func<T, T, double> similarity,
        int k,
        double lambda = 0.5)
    {
        var docs = documents.ToList();
        var selected = new List<T>();

        while (selected.Count < k && docs.Count > 0)
        {
            var bestDoc = docs[0];
            var bestScore = double.MinValue;

            foreach (var doc in docs)
            {
                var relScore = relevance(doc, default!);
                var simScore = selected.Count == 0 ? 0 : selected.Min(s => similarity(doc, s));
                var mmrScore = lambda * relScore - (1 - lambda) * simScore;

                if (mmrScore > bestScore)
                {
                    bestScore = mmrScore;
                    bestDoc = doc;
                }
            }

            selected.Add(bestDoc);
            docs.Remove(bestDoc);
        }

        return selected;
    }
}
