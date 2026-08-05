namespace Shared.AI.EngineClient;

/// <summary>
/// Base exception for AI Engine client failures.
/// </summary>
public class AiEngineClientException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="AiEngineClientException"/> class.</summary>
    public AiEngineClientException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="AiEngineClientException"/> class.</summary>
    public AiEngineClientException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when the server returns a failure response.
/// </summary>
public sealed class AiEngineRemoteException : AiEngineClientException
{
    /// <summary>Initializes a new instance of the <see cref="AiEngineRemoteException"/> class.</summary>
    public AiEngineRemoteException(string message, IReadOnlyDictionary<string, string>? metadata = null)
        : base(message)
    {
        Metadata = metadata ?? new Dictionary<string, string>();
    }

    /// <summary>Gets server metadata.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}

/// <summary>
/// Thrown when the response payload cannot be parsed.
/// </summary>
public sealed class AiEngineProtocolException : AiEngineClientException
{
    /// <summary>Initializes a new instance of the <see cref="AiEngineProtocolException"/> class.</summary>
    public AiEngineProtocolException(string message, Exception? innerException = null) : base(message, innerException ?? new Exception(message)) { }
}

