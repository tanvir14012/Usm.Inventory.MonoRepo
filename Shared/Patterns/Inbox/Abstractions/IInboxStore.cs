namespace Usm.Shared.Patterns.Inbox.Abstractions;

/// <summary>
/// Stores deduplication keys for inbox processing.
/// </summary>
/// <typeparam name="TKey">The deduplication key type.</typeparam>
public interface IInboxStore<TKey>
{
    /// <summary>Gets the number of tracked keys.</summary>
    int Count { get; }

    /// <summary>Attempts to register a key for processing.</summary>
    ValueTask<bool> TryRegisterAsync(TKey key, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    /// <summary>Marks a key as processed.</summary>
    ValueTask MarkProcessedAsync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>Removes a key from the store.</summary>
    ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>Removes expired keys.</summary>
    ValueTask<int> CleanupExpiredAsync(DateTimeOffset utcNow, CancellationToken cancellationToken = default);
}
