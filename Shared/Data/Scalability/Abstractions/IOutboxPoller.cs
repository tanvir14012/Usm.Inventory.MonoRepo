namespace Usm.Shared.Data.Scalability.Abstractions;

/// <summary>
/// Outbox message polling contract.
/// Implementations use <c>SELECT … FOR UPDATE SKIP LOCKED</c> to prevent
/// concurrent workers from processing the same message.
/// </summary>
public interface IOutboxPoller<TMessage> where TMessage : class
{
    /// <summary>Locks and returns up to <paramref name="batchSize"/> pending messages.</summary>
    ValueTask<IReadOnlyList<TMessage>> PollAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>Marks messages as successfully processed.</summary>
    ValueTask AcknowledgeAsync(IReadOnlyList<TMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>Increments retry count and records the failure reason for later re-processing.</summary>
    ValueTask NackAsync(TMessage message, string reason, CancellationToken cancellationToken = default);
}
