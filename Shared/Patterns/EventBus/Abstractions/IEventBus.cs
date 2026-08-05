using Usm.Shared.Patterns.EventBus;

namespace Usm.Shared.Patterns.EventBus.Abstractions;

/// <summary>
/// Describes an asynchronous event bus.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
public interface IEventBus<TEvent>
{
    /// <summary>Publishes an event synchronously.</summary>
    void Publish(TEvent @event, CancellationToken cancellationToken = default);

    /// <summary>Publishes an event asynchronously.</summary>
    ValueTask PublishAsync(TEvent @event, CancellationToken cancellationToken = default);
}
