using Usm.Shared.Patterns.Outbox;

namespace Usm.Shared.Patterns.Outbox.Abstractions;

/// <summary>
/// Stores outbox messages until they are dispatched.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface IOutboxStore<TMessage>
{
    /// <summary>Gets the number of pending messages.</summary>
    int Count { get; }

    /// <summary>Adds a message payload to the store.</summary>
    ValueTask EnqueueAsync(byte[] payload, CancellationToken cancellationToken = default);

    /// <summary>Dequeues a batch of payloads for dispatch.</summary>
    ValueTask<IReadOnlyList<OutboxRecord>> DequeueBatchAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>Marks a record as dispatched.</summary>
    ValueTask MarkDispatchedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Requeues a record after a failed dispatch.</summary>
    ValueTask RequeueAsync(OutboxRecord record, CancellationToken cancellationToken = default);
}
