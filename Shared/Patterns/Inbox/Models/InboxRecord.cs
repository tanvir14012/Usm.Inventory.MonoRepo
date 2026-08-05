namespace Usm.Shared.Patterns.Inbox;

/// <summary>
/// Represents a tracked inbox key and its lifecycle timestamps.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
public sealed record InboxRecord<TKey>(
    TKey Key,
    DateTimeOffset RegisteredAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ProcessedAt,
    int Attempts);
