using Usm.Shared.Patterns.Outbox;
using Usm.Shared.Patterns.Outbox.Abstractions;
using Usm.Shared.Patterns.Outbox.Extensions;

namespace Usm.Shared.Patterns.Outbox.Builders;

/// <summary>
/// Fluent builder for an outbox.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public sealed class OutboxBuilder<TMessage> : IOutboxBuilder<TMessage>
{
    private IOutboxStore<TMessage>? _store;
    private IOutboxSerializer<TMessage>? _serializer;
    private IOutboxDispatcher<TMessage>? _dispatcher;
    private readonly OutboxOptions _options = new();

    /// <inheritdoc />
    public IOutboxBuilder<TMessage> WithStore(IOutboxStore<TMessage> store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        return this;
    }

    /// <inheritdoc />
    public IOutboxBuilder<TMessage> WithSerializer(IOutboxSerializer<TMessage> serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        return this;
    }

    /// <inheritdoc />
    public IOutboxBuilder<TMessage> WithDispatcher(IOutboxDispatcher<TMessage> dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        return this;
    }

    /// <inheritdoc />
    public IOutboxBuilder<TMessage> WithBatchSize(int batchSize)
    {
        _options.BatchSize = batchSize > 0 ? batchSize : throw new ArgumentOutOfRangeException(nameof(batchSize));
        return this;
    }

    /// <inheritdoc />
    public IOutboxBuilder<TMessage> WithRetention(TimeSpan retention)
    {
        _options.Retention = retention > TimeSpan.Zero ? retention : throw new ArgumentOutOfRangeException(nameof(retention));
        return this;
    }

    /// <inheritdoc />
    public IOutbox<TMessage> Build()
        => new Outbox<TMessage>(
            _store ?? new InMemoryOutboxStore<TMessage>(),
            _serializer ?? new SystemTextJsonOutboxSerializer<TMessage>(),
            _dispatcher ?? throw new InvalidOperationException("An outbox dispatcher is required."),
            _options);
}
