namespace Usm.Shared.Data.Scalability.Polling;

/// <summary>
/// Minimal contract for outbox message entities.
/// Implement this on your EF Core entity class to use the built-in
/// <c>EfCoreOutboxPoller&lt;TDbContext, TMessage&gt;</c>.
/// </summary>
public interface IOutboxMessage
{
    Guid Id { get; }
    string Type { get; }
    string Payload { get; }
    int RetryCount { get; set; }
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset? ProcessedAt { get; set; }
    string? Error { get; set; }
}
