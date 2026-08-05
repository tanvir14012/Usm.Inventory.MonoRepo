namespace Usm.Shared.Patterns.Outbox.Abstractions;

/// <summary>
/// Describes an outbox that stores and dispatches messages reliably.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface IOutbox<TMessage>
{
    /// <summary>Gets the number of pending messages.</summary>
    int PendingCount { get; }

    /// <summary>Enqueues a message for later dispatch.</summary>
    ValueTask EnqueueAsync(TMessage message, CancellationToken cancellationToken = default);

    /// <summary>Dispatches pending messages in batches.</summary>
    ValueTask<int> DispatchPendingAsync(CancellationToken cancellationToken = default);
}
