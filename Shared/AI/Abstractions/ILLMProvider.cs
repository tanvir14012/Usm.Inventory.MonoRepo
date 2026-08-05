namespace Shared.AI.Abstractions;

/// <summary>
/// Represents the role of a chat message participant.
/// </summary>
public enum MessageRole
{
    /// <summary>System instructions for the AI model.</summary>
    System,
    
    /// <summary>Message from the user.</summary>
    User,
    
    /// <summary>Response from the assistant/AI.</summary>
    Assistant,
    
    /// <summary>Output from function/tool calls.</summary>
    Tool
}

/// <summary>
/// Represents a single message in a conversation.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessage"/> class.
    /// </summary>
    /// <param name="role">The role of the message sender.</param>
    /// <param name="content">The message content.</param>
    /// <param name="toolCallId">Optional ID if this is a tool response.</param>
    public ChatMessage(MessageRole role, string content, string? toolCallId = null)
    {
        Role = role;
        Content = content;
        ToolCallId = toolCallId;
    }

    /// <summary>Gets the role of the message sender.</summary>
    public MessageRole Role { get; }

    /// <summary>Gets the message content.</summary>
    public string Content { get; }

    /// <summary>Gets the tool call ID if this is a tool response.</summary>
    public string? ToolCallId { get; }

    /// <summary>
    /// Creates a system message.
    /// </summary>
    public static ChatMessage System(string content) => new(MessageRole.System, content);

    /// <summary>
    /// Creates a user message.
    /// </summary>
    public static ChatMessage User(string content) => new(MessageRole.User, content);

    /// <summary>
    /// Creates an assistant message.
    /// </summary>
    public static ChatMessage Assistant(string content) => new(MessageRole.Assistant, content);

    /// <summary>
    /// Creates a tool response message.
    /// </summary>
    public static ChatMessage Tool(string content, string toolCallId) => 
        new(MessageRole.Tool, content, toolCallId);
}

/// <summary>
/// Represents a response from an LLM provider.
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatResponse"/> class.
    /// </summary>
    public ChatResponse(string content, string? model = null, int? inputTokens = null, int? outputTokens = null)
    {
        Content = content;
        Model = model;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
    }

    /// <summary>Gets the generated response content.</summary>
    public string Content { get; }

    /// <summary>Gets the model used for generation.</summary>
    public string? Model { get; }

    /// <summary>Gets the number of input tokens used.</summary>
    public int? InputTokens { get; }

    /// <summary>Gets the number of output tokens generated.</summary>
    public int? OutputTokens { get; }

    /// <summary>Gets the total tokens used.</summary>
    public int? TotalTokens => (InputTokens ?? 0) + (OutputTokens ?? 0);
}

/// <summary>
/// Configuration for an LLM provider.
/// </summary>
public interface ILLMProviderConfig
{
    /// <summary>Gets the provider name.</summary>
    string ProviderName { get; }

    /// <summary>Gets the model identifier.</summary>
    string Model { get; }

    /// <summary>Gets the API key.</summary>
    string? ApiKey { get; }

    /// <summary>Gets the endpoint URL.</summary>
    string? Endpoint { get; }

    /// <summary>Gets the temperature (0-2).</summary>
    double? Temperature { get; }

    /// <summary>Gets the maximum tokens to generate.</summary>
    int? MaxTokens { get; }

    /// <summary>Gets the top-p sampling parameter.</summary>
    double? TopP { get; }

    /// <summary>Gets custom headers.</summary>
    IReadOnlyDictionary<string, string>? CustomHeaders { get; }
}

/// <summary>
/// Base interface for LLM providers (OpenAI, Azure, Claude, etc).
/// Provides abstraction over different AI service providers.
/// </summary>
public interface ILLMProvider : IAsyncDisposable
{
    /// <summary>Gets the configuration for this provider.</summary>
    ILLMProviderConfig Config { get; }

    /// <summary>
    /// Sends a chat completion request.
    /// </summary>
    /// <param name="messages">The conversation messages.</param>
    /// <param name="options">Optional completion options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chat response.</returns>
    Task<ChatResponse> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a chat completion response.
    /// </summary>
    /// <param name="messages">The conversation messages.</param>
    /// <param name="options">Optional completion options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of response chunks.</returns>
    IAsyncEnumerable<string> StreamCompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the provider connection and authentication.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection is successful.</returns>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for chat completion requests.
/// </summary>
public class ChatCompletionOptions
{
    /// <summary>Gets or sets the temperature.</summary>
    public double? Temperature { get; set; }

    /// <summary>Gets or sets the maximum tokens to generate.</summary>
    public int? MaxTokens { get; set; }

    /// <summary>Gets or sets the top-p sampling parameter.</summary>
    public double? TopP { get; set; }

    /// <summary>Gets or sets the frequency penalty.</summary>
    public double? FrequencyPenalty { get; set; }

    /// <summary>Gets or sets the presence penalty.</summary>
    public double? PresencePenalty { get; set; }

    /// <summary>Gets or sets stop sequences.</summary>
    public IReadOnlyList<string>? StopSequences { get; set; }

    /// <summary>Gets or sets the number of top choices to return.</summary>
    public int? TopK { get; set; }

    /// <summary>Gets or sets a timeout for the request.</summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>Gets or sets whether to require structured JSON output.</summary>
    public bool? RequireStructuredOutput { get; set; }

    /// <summary>Gets or sets the JSON schema for structured output.</summary>
    public string? JsonSchema { get; set; }
}
