namespace Shared.AI.Agents;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.AI.Abstractions;
using Shared.AI.Memory;

/// <summary>
/// Base agent that can interact with tools and maintain conversation state.
/// </summary>
public class Agent
{
    private readonly string _name;
    private readonly IChatService _chatService;
    private readonly IToolRegistry _toolRegistry;
    private readonly ConversationMemory _memory;
    private readonly ILogger? _logger;

    public Agent(
        string name,
        IChatService chatService,
        IToolRegistry toolRegistry,
        ILogger? logger = null)
    {
        _name = name;
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _memory = new ConversationMemory();
        _logger = logger;
    }

    /// <summary>
    /// Gets the agent name.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Gets available tools.
    /// </summary>
    public IReadOnlyList<ITool> GetTools() => _toolRegistry.GetAllTools();

    /// <summary>
    /// Executes a single turn of the agent loop.
    /// </summary>
    public async Task<string> ExecuteAsync(
        string input,
        int maxIterations = 5,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Agent {AgentName} executing: {Input}", _name, input);

        _memory.AddMessage(ChatMessage.User(input));

        for (int i = 0; i < maxIterations; i++)
        {
            var messages = _memory.GetMessages();
            var response = await _chatService.SendAsync(messages, cancellationToken: cancellationToken);

            _memory.AddMessage(ChatMessage.Assistant(response.Content));

            // Check if response contains tool calls (simplified check)
            if (!response.Content.Contains("[TOOL:") && !response.Content.Contains("<tool>"))
            {
                _logger?.LogDebug("Agent {AgentName} finished after {Iterations} iterations", _name, i + 1);
                return response.Content;
            }

            // Parse and execute tool calls
            var toolCalls = ParseToolCalls(response.Content);
            if (toolCalls.Count == 0)
            {
                return response.Content;
            }

            foreach (var (toolName, args) in toolCalls)
            {
                try
                {
                    var result = await _toolRegistry.ExecuteToolAsync(toolName, args, cancellationToken);
                    _memory.AddMessage(ChatMessage.Tool(result, toolName));
                    _logger?.LogDebug("Agent {AgentName} executed tool {Tool}", _name, toolName);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Tool {Tool} execution failed", toolName);
                    _memory.AddMessage(ChatMessage.Tool($"Error: {ex.Message}", toolName));
                }
            }
        }

        return "Max iterations reached";
    }

    /// <summary>
    /// Streams agent response.
    /// </summary>
    public async IAsyncEnumerable<string> ExecuteStreamAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        _memory.AddMessage(ChatMessage.User(input));

        var messages = _memory.GetMessages();
        await foreach (var chunk in _chatService.StreamAsync(messages, cancellationToken: cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Clears conversation memory.
    /// </summary>
    public void ClearMemory()
    {
        _memory.Clear();
        _logger?.LogDebug("Agent {AgentName} memory cleared", _name);
    }

    private List<(string toolName, string args)> ParseToolCalls(string response)
    {
        var toolCalls = new List<(string, string)>();

        // Parse [TOOL: name(args)] format
        var pattern = new System.Text.RegularExpressions.Regex(@"\[TOOL:\s*(\w+)\s*\(([^)]*)\)\s*\]");
        foreach (System.Text.RegularExpressions.Match match in pattern.Matches(response))
        {
            var toolName = match.Groups[1].Value;
            var args = match.Groups[2].Value;
            toolCalls.Add((toolName, args));
        }

        return toolCalls;
    }
}

/// <summary>
/// Specialized agent for routing requests to different handlers.
/// </summary>
public class RouterAgent
{
    private readonly Dictionary<string, Func<string, Task<string>>> _routes = new();
    private readonly IChatService _chatService;
    private readonly ILogger? _logger;

    public RouterAgent(IChatService chatService, ILogger? logger = null)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _logger = logger;
    }

    /// <summary>
    /// Registers a route handler.
    /// </summary>
    public void RegisterRoute(string name, string description, Func<string, Task<string>> handler)
    {
        _routes[name] = handler;
        _logger?.LogDebug("Registered route: {RouteName}", name);
    }

    /// <summary>
    /// Routes the input to the appropriate handler.
    /// </summary>
    public async Task<string> RouteAsync(string input, CancellationToken cancellationToken = default)
    {
        var routeDescriptions = string.Join("\n", _routes.Keys.Select(r => $"- {r}"));
        var routingPrompt = $"Determine which of these routes the following input should go to:\n{routeDescriptions}\n\nInput: {input}\n\nRoute:";

        var response = await _chatService.SendAsync(routingPrompt, cancellationToken: cancellationToken);
        var routeName = ExtractRouteName(response.Content);

        if (string.IsNullOrEmpty(routeName) || !_routes.TryGetValue(routeName, out var handler))
        {
            _logger?.LogWarning("No route found for input: {Input}", input);
            return $"Could not determine appropriate route for: {input}";
        }

        _logger?.LogDebug("Routing to: {RouteName}", routeName);
        return await handler(input);
    }

    private string ExtractRouteName(string response)
    {
        var words = response.Split(new[] { ' ', '\n', '.', '-' }, StringSplitOptions.RemoveEmptyEntries);
        return words.FirstOrDefault(w => _routes.ContainsKey(w)) ?? string.Empty;
    }
}

/// <summary>
/// Multi-agent framework for complex tasks.
/// </summary>
public class MultiAgentOrchestrator
{
    private readonly Dictionary<string, Agent> _agents = new();
    private readonly ILogger? _logger;

    public MultiAgentOrchestrator(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers an agent.
    /// </summary>
    public void RegisterAgent(Agent agent)
    {
        _agents[agent.Name] = agent;
        _logger?.LogDebug("Registered agent: {AgentName}", agent.Name);
    }

    /// <summary>
    /// Executes a task across multiple agents.
    /// </summary>
    public async Task<string> ExecuteAsync(
        string input,
        params string[] agentNames)
    {
        var results = new List<(string agentName, string result)>();

        foreach (var agentName in agentNames)
        {
            if (!_agents.TryGetValue(agentName, out var agent))
            {
                _logger?.LogWarning("Agent not found: {AgentName}", agentName);
                continue;
            }

            var result = await agent.ExecuteAsync(input);
            results.Add((agentName, result));
            _logger?.LogDebug("Agent {AgentName} completed", agentName);
        }

        return string.Join("\n\n", results.Select(r => $"[{r.agentName}]\n{r.result}"));
    }

    /// <summary>
    /// Gets an agent by name.
    /// </summary>
    public Agent? GetAgent(string name)
    {
        _agents.TryGetValue(name, out var agent);
        return agent;
    }
}
