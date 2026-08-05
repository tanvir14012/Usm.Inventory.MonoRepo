namespace Usm.Shared.Patterns.Inbox.Abstractions;

/// <summary>
/// Processes messages with idempotency and deduplication.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
/// <typeparam name="TKey">The deduplication key type.</typeparam>
public interface IInbox<TMessage, TKey>
{
    /// <summary>Gets the number of tracked keys.</summary>
    int PendingCount { get; }

    /// <summary>Processes a message if it has not been seen before.</summary>
    ValueTask<bool> ProcessAsync(TMessage message, CancellationToken cancellationToken = default);

    /// <summary>Removes expired deduplication entries.</summary>
    ValueTask<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}
