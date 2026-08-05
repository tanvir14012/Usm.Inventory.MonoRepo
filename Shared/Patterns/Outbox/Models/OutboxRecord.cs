namespace Usm.Shared.Patterns.Outbox;

/// <summary>
/// Represents a persisted outbox record.
/// </summary>
public sealed record OutboxRecord(Guid Id, byte[] Payload, DateTimeOffset EnqueuedAt, int Attempts);
