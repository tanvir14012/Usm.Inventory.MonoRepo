namespace Usm.Shared.Patterns.Inbox.Abstractions;

/// <summary>
/// Handles a message once it has passed inbox deduplication.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface IInboxHandler<TMessage>
{
    /// <summary>Processes the message.</summary>
    ValueTask HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}
