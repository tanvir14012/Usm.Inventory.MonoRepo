using Usm.Shared.Patterns.EventBus;
using Usm.Shared.Patterns.EventBus.Abstractions;
using Usm.Shared.Patterns.EventBus.Extensions;

namespace Usm.Shared.Patterns.EventBus.Builders;

/// <summary>
/// Fluent builder for an event bus.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
public sealed class EventBusBuilder<TEvent> : IEventBusBuilder<TEvent>
{
    private readonly List<EventSubscription<TEvent>> _subscriptions = [];
    private readonly List<Func<TEvent, Func<CancellationToken, ValueTask>, CancellationToken, ValueTask>> _middlewares = [];
    private EventBusOptions _options = new();

    /// <inheritdoc />
    public IEventBusBuilder<TEvent> Subscribe(Action<TEvent> handler, int priority = 0, string? group = null)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _subscriptions.Add(EventSubscription<TEvent>.FromHandler(handler, priority, group));
        return this;
    }

    /// <inheritdoc />
    public IEventBusBuilder<TEvent> SubscribeAsync(Func<TEvent, CancellationToken, ValueTask> handler, int priority = 0, string? group = null)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _subscriptions.Add(EventSubscription<TEvent>.FromAsyncHandler(handler, priority, group));
        return this;
    }

    /// <inheritdoc />
    public IEventBusBuilder<TEvent> Use(Func<TEvent, Func<CancellationToken, ValueTask>, CancellationToken, ValueTask> middleware)
    {
        _middlewares.Add(middleware ?? throw new ArgumentNullException(nameof(middleware)));
        return this;
    }

    /// <inheritdoc />
    public IEventBusBuilder<TEvent> WithDispatchMode(EventDispatchMode mode)
    {
        _options.DispatchMode = mode;
        return this;
    }

    /// <inheritdoc />
    public IEventBus<TEvent> Build()
        => new EventBus<TEvent>(_subscriptions, _middlewares, _options);
}
