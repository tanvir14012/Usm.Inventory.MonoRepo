namespace Shared.AI.Python;

public sealed class TransformersWrapper
{
    private readonly PersistentPythonBridge _bridge;

    public TransformersWrapper(PersistentPythonBridge bridge)
    {
        _bridge = bridge;
    }

    public Task<float[]> GetEmbeddingAsync(string text, string model = "sentence-transformers/all-MiniLM-L6-v2", CancellationToken cancellationToken = default)
        => _bridge.GetEmbeddingAsync(text, model, cancellationToken);

    public Task<string> ClassifyTextAsync(string text, string model = "distilbert-base-uncased-finetuned-sst-2-english", CancellationToken cancellationToken = default)
        => _bridge.InvokeAsync<string>(PythonRequestFactory.Classification(text, model), cancellationToken);
}

public sealed class spaCyWrapper
{
    private readonly PersistentPythonBridge _bridge;

    public spaCyWrapper(PersistentPythonBridge bridge)
    {
        _bridge = bridge;
    }

    public Task<IReadOnlyDictionary<string, List<string>>> ExtractEntitiesAsync(string text, string model = "en_core_web_sm", CancellationToken cancellationToken = default)
        => _bridge.ExtractEntitiesAsync(text, model, cancellationToken);
}
