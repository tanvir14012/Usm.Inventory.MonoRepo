using Usm.Shared.Patterns.Outbox;

namespace Usm.Shared.Patterns.Outbox.Abstractions;

/// <summary>
/// Fluent builder for configuring an outbox.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface IOutboxBuilder<TMessage>
{
    /// <summary>Sets the backing store.</summary>
    IOutboxBuilder<TMessage> WithStore(IOutboxStore<TMessage> store);

    /// <summary>Sets the serializer.</summary>
    IOutboxBuilder<TMessage> WithSerializer(IOutboxSerializer<TMessage> serializer);

    /// <summary>Sets the dispatcher.</summary>
    IOutboxBuilder<TMessage> WithDispatcher(IOutboxDispatcher<TMessage> dispatcher);

    /// <summary>Sets the batch size.</summary>
    IOutboxBuilder<TMessage> WithBatchSize(int batchSize);

    /// <summary>Sets the retention period.</summary>
    IOutboxBuilder<TMessage> WithRetention(TimeSpan retention);

    /// <summary>Builds the outbox.</summary>
    IOutbox<TMessage> Build();
}
