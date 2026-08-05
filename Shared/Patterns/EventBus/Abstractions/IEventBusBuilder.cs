using Usm.Shared.Patterns.EventBus;

namespace Usm.Shared.Patterns.EventBus.Abstractions;

/// <summary>
/// Fluent builder for configuring an event bus.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
public interface IEventBusBuilder<TEvent>
{
    /// <summary>Subscribes a synchronous handler.</summary>
    IEventBusBuilder<TEvent> Subscribe(Action<TEvent> handler, int priority = 0, string? group = null);

    /// <summary>Subscribes an asynchronous handler.</summary>
    IEventBusBuilder<TEvent> SubscribeAsync(Func<TEvent, CancellationToken, ValueTask> handler, int priority = 0, string? group = null);

    /// <summary>Adds middleware around event dispatch.</summary>
    IEventBusBuilder<TEvent> Use(Func<TEvent, Func<CancellationToken, ValueTask>, CancellationToken, ValueTask> middleware);

    /// <summary>Sets the dispatch mode.</summary>
    IEventBusBuilder<TEvent> WithDispatchMode(EventDispatchMode mode);

    /// <summary>Builds the event bus.</summary>
    IEventBus<TEvent> Build();
}
