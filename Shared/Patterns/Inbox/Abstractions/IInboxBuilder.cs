namespace Usm.Shared.Patterns.Inbox.Abstractions;

/// <summary>
/// Fluent builder for configuring an inbox.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
/// <typeparam name="TKey">The deduplication key type.</typeparam>
public interface IInboxBuilder<TMessage, TKey>
{
    /// <summary>Sets the backing store.</summary>
    IInboxBuilder<TMessage, TKey> WithStore(IInboxStore<TKey> store);

    /// <summary>Sets the message handler.</summary>
    IInboxBuilder<TMessage, TKey> WithHandler(IInboxHandler<TMessage> handler);

    /// <summary>Sets the key selector used for deduplication.</summary>
    IInboxBuilder<TMessage, TKey> WithKeySelector(Func<TMessage, TKey> keySelector);

    /// <summary>Sets the retention period for processed keys.</summary>
    IInboxBuilder<TMessage, TKey> WithRetention(TimeSpan retention);

    /// <summary>Builds the inbox.</summary>
    IInbox<TMessage, TKey> Build();
}
