namespace Usm.Shared.Patterns.EventBus;

/// <summary>
/// Supported event dispatch modes.
/// </summary>
public enum EventDispatchMode
{
    /// <summary>Dispatch handlers sequentially.</summary>
    Sequential = 0,

    /// <summary>Dispatch handlers in parallel.</summary>
    Parallel = 1
}

/// <summary>
/// Configuration for the event bus.
/// </summary>
public sealed class EventBusOptions
{
    /// <summary>Gets or sets the dispatch mode.</summary>
    public EventDispatchMode DispatchMode { get; set; } = EventDispatchMode.Sequential;

    /// <summary>Gets or sets a value indicating whether an exception should be thrown when no handlers are registered.</summary>
    public bool ThrowIfNoSubscribers { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether dispatch should stop on the first handler failure.</summary>
    public bool FailFast { get; set; } = true;
}
