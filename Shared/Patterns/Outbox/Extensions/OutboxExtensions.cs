using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.Outbox;
using Usm.Shared.Patterns.Outbox.Abstractions;
using Usm.Shared.Patterns.Outbox.Builders;

namespace Usm.Shared.Patterns.Outbox.Extensions;

/// <summary>
/// Common extension methods for outbox creation and DI registration.
/// </summary>
public static class OutboxExtensions
{
    /// <summary>Registers the outbox framework with dependency injection.</summary>
    public static IServiceCollection AddOutboxFramework(this IServiceCollection services)
    {
        services.AddOptions<OutboxOptions>();
        services.TryAddTransient(typeof(OutboxBuilder<>), typeof(OutboxBuilder<>));
        services.TryAddSingleton(typeof(IOutboxStore<>), typeof(InMemoryOutboxStore<>));
        services.TryAddSingleton(typeof(IOutboxSerializer<>), typeof(SystemTextJsonOutboxSerializer<>));
        return services;
    }
}

/// <summary>
/// Default JSON serializer for outbox messages.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public sealed class SystemTextJsonOutboxSerializer<TMessage> : IOutboxSerializer<TMessage>
{
    /// <inheritdoc />
    public byte[] Serialize(TMessage message)
        => JsonSerializer.SerializeToUtf8Bytes(message);

    /// <inheritdoc />
    public TMessage Deserialize(byte[] payload)
        => JsonSerializer.Deserialize<TMessage>(payload) ?? throw new InvalidOperationException("Unable to deserialize outbox payload.");
}

/// <summary>
/// In-memory outbox store used for tests and local development.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public sealed class InMemoryOutboxStore<TMessage> : IOutboxStore<TMessage>
{
    private readonly Queue<OutboxRecord> _queue = new();
    private readonly object _gate = new();

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_gate)
                return _queue.Count;
        }
    }

    /// <inheritdoc />
    public ValueTask EnqueueAsync(byte[] payload, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _queue.Enqueue(new OutboxRecord(Guid.NewGuid(), payload, DateTimeOffset.UtcNow, 0));
            return ValueTask.CompletedTask;
        }
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<OutboxRecord>> DequeueBatchAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var batch = new List<OutboxRecord>(Math.Min(batchSize, _queue.Count));
            while (batch.Count < batchSize && _queue.Count > 0)
                batch.Add(_queue.Dequeue());

            return ValueTask.FromResult((IReadOnlyList<OutboxRecord>)batch);
        }
    }

    /// <inheritdoc />
    public ValueTask MarkDispatchedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask RequeueAsync(OutboxRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _queue.Enqueue(record with { Attempts = record.Attempts + 1 });
            return ValueTask.CompletedTask;
        }
    }
}

internal sealed class Outbox<TMessage> : IOutbox<TMessage>
{
    private readonly IOutboxStore<TMessage> _store;
    private readonly IOutboxSerializer<TMessage> _serializer;
    private readonly IOutboxDispatcher<TMessage> _dispatcher;
    private readonly OutboxOptions _options;

    public Outbox(
        IOutboxStore<TMessage> store,
        IOutboxSerializer<TMessage> serializer,
        IOutboxDispatcher<TMessage> dispatcher,
        OutboxOptions options)
    {
        _store = store;
        _serializer = serializer;
        _dispatcher = dispatcher;
        _options = options;
    }

    public int PendingCount => _store.Count;

    public ValueTask EnqueueAsync(TMessage message, CancellationToken cancellationToken = default)
        => _store.EnqueueAsync(_serializer.Serialize(message), cancellationToken);

    public async ValueTask<int> DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        var dispatched = 0;
        var batch = await _store.DequeueBatchAsync(_options.BatchSize, cancellationToken).ConfigureAwait(false);

        foreach (var record in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var message = _serializer.Deserialize(record.Payload);
                await _dispatcher.DispatchAsync(message, cancellationToken).ConfigureAwait(false);
                await _store.MarkDispatchedAsync(record.Id, cancellationToken).ConfigureAwait(false);
                dispatched++;
            }
            catch
            {
                await _store.RequeueAsync(record, cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        return dispatched;
    }
}
