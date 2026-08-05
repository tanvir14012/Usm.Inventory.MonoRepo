using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.Inbox.Abstractions;
using Usm.Shared.Patterns.Inbox.Builders;

namespace Usm.Shared.Patterns.Inbox.Extensions;

/// <summary>
/// Common extension methods for inbox registration.
/// </summary>
public static class InboxExtensions
{
    /// <summary>Registers the inbox framework with dependency injection.</summary>
    public static IServiceCollection AddInboxFramework(this IServiceCollection services)
    {
        services.AddOptions<InboxOptions>();
        services.TryAddSingleton(typeof(IInboxStore<>), typeof(InMemoryInboxStore<>));
        services.TryAddTransient(typeof(IInboxBuilder<,>), typeof(InboxBuilder<,>));
        return services;
    }
}

internal sealed class Inbox<TMessage, TKey> : IInbox<TMessage, TKey>
    where TKey : notnull
{
    private readonly IInboxStore<TKey> _store;
    private readonly IInboxHandler<TMessage> _handler;
    private readonly Func<TMessage, TKey> _keySelector;
    private readonly InboxOptions _options;
    private readonly ILogger<IInbox<TMessage, TKey>> _logger;

    public Inbox(
        IInboxStore<TKey> store,
        IInboxHandler<TMessage> handler,
        Func<TMessage, TKey> keySelector,
        InboxOptions options,
        ILogger<IInbox<TMessage, TKey>> logger)
    {
        _store = store;
        _handler = handler;
        _keySelector = keySelector;
        _options = options;
        _logger = logger;
    }

    public int PendingCount => _store.Count;

    public async ValueTask<bool> ProcessAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = _keySelector(message);
        var expiresAt = DateTimeOffset.UtcNow.Add(_options.Retention);
        if (!await _store.TryRegisterAsync(key, expiresAt, cancellationToken).ConfigureAwait(false))
            return false;

        try
        {
            await _handler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
            await _store.MarkProcessedAsync(key, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Inbox processed message with key {InboxKey}.", key);
            return true;
        }
        catch
        {
            await _store.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public ValueTask<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
        => _store.CleanupExpiredAsync(DateTimeOffset.UtcNow, cancellationToken);
}

/// <summary>
/// Default in-memory inbox store used for tests and local development.
/// </summary>
/// <typeparam name="TKey">The deduplication key type.</typeparam>
public sealed class InMemoryInboxStore<TKey> : IInboxStore<TKey>
    where TKey : notnull
{
    private readonly Dictionary<TKey, InboxRecord<TKey>> _records;
    private readonly object _gate = new();

    /// <summary>Initializes a new store.</summary>
    public InMemoryInboxStore()
        : this(EqualityComparer<TKey>.Default)
    {
    }

    /// <summary>Initializes a new store with a custom comparer.</summary>
    public InMemoryInboxStore(IEqualityComparer<TKey> comparer)
    {
        _records = new Dictionary<TKey, InboxRecord<TKey>>(comparer ?? EqualityComparer<TKey>.Default);
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_gate)
                return _records.Count;
        }
    }

    /// <inheritdoc />
    public ValueTask<bool> TryRegisterAsync(TKey key, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_records.TryGetValue(key, out var record) && record.ExpiresAt > DateTimeOffset.UtcNow)
                return ValueTask.FromResult(false);

            _records[key] = new InboxRecord<TKey>(key, DateTimeOffset.UtcNow, expiresAt, null, 0);
            return ValueTask.FromResult(true);
        }
    }

    /// <inheritdoc />
    public ValueTask MarkProcessedAsync(TKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_records.TryGetValue(key, out var record))
                _records[key] = record with { ProcessedAt = DateTimeOffset.UtcNow, Attempts = record.Attempts + 1 };
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
            _records.Remove(key);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<int> CleanupExpiredAsync(DateTimeOffset utcNow, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var removed = 0;
            var keys = new List<TKey>();

            foreach (var pair in _records)
            {
                if (pair.Value.ExpiresAt <= utcNow)
                    keys.Add(pair.Key);
            }

            for (var i = 0; i < keys.Count; i++)
            {
                if (_records.Remove(keys[i]))
                    removed++;
            }

            return ValueTask.FromResult(removed);
        }
    }
}
