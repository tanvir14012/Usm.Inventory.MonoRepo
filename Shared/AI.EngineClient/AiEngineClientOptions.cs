namespace Shared.AI.EngineClient;

/// <summary>
/// Options for the AI Engine gRPC client.
/// </summary>
public sealed class AiEngineClientOptions
{
    /// <summary>Gets or sets the default per-request timeout.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>Gets or sets the maximum receive message size.</summary>
    public int MaxReceiveMessageSize { get; set; } = 16 * 1024 * 1024;

    /// <summary>Gets or sets the maximum send message size.</summary>
    public int MaxSendMessageSize { get; set; } = 16 * 1024 * 1024;

    /// <summary>Gets or sets the default metadata to attach to every request.</summary>
    public Dictionary<string, string> DefaultMetadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates the JSON serializer options used for payload serialization.</summary>
    public static JsonSerializerOptions CreateSerializerOptions()
    {
        return new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }
}

