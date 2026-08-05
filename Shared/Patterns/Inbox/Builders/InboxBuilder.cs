using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usm.Shared.Patterns.Inbox.Abstractions;
using Usm.Shared.Patterns.Inbox.Extensions;

namespace Usm.Shared.Patterns.Inbox.Builders;

/// <summary>
/// Fluent builder for inbox configuration.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
/// <typeparam name="TKey">The deduplication key type.</typeparam>
public sealed class InboxBuilder<TMessage, TKey> : IInboxBuilder<TMessage, TKey>
    where TKey : notnull
{
    private IInboxStore<TKey>? _store;
    private IInboxHandler<TMessage>? _handler;
    private Func<TMessage, TKey>? _keySelector;
    private ILogger<Usm.Shared.Patterns.Inbox.Abstractions.IInbox<TMessage, TKey>>? _logger;
    private readonly InboxOptions _options = new();

    /// <inheritdoc />
    public IInboxBuilder<TMessage, TKey> WithStore(IInboxStore<TKey> store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        return this;
    }

    /// <inheritdoc />
    public IInboxBuilder<TMessage, TKey> WithHandler(IInboxHandler<TMessage> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }

    /// <inheritdoc />
    public IInboxBuilder<TMessage, TKey> WithKeySelector(Func<TMessage, TKey> keySelector)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        return this;
    }

    /// <inheritdoc />
    public IInboxBuilder<TMessage, TKey> WithRetention(TimeSpan retention)
    {
        _options.Retention = retention > TimeSpan.Zero ? retention : throw new ArgumentOutOfRangeException(nameof(retention));
        return this;
    }

    /// <summary>Sets the logger used by the inbox.</summary>
    public IInboxBuilder<TMessage, TKey> WithLogger(ILogger<Usm.Shared.Patterns.Inbox.Abstractions.IInbox<TMessage, TKey>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        return this;
    }

    /// <inheritdoc />
    public IInbox<TMessage, TKey> Build()
        => new Inbox<TMessage, TKey>(
            _store ?? new InMemoryInboxStore<TKey>(),
            _handler ?? throw new InvalidOperationException("An inbox handler is required."),
            _keySelector ?? throw new InvalidOperationException("A key selector is required."),
            _options,
            _logger ?? NullLogger<Usm.Shared.Patterns.Inbox.Abstractions.IInbox<TMessage, TKey>>.Instance);
}
