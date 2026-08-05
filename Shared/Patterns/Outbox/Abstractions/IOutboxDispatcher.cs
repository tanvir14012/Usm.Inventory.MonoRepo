namespace Usm.Shared.Patterns.Outbox.Abstractions;

/// <summary>
/// Dispatches a message to an external destination.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface IOutboxDispatcher<TMessage>
{
    /// <summary>Dispatches the message.</summary>
    ValueTask DispatchAsync(TMessage message, CancellationToken cancellationToken = default);
}
