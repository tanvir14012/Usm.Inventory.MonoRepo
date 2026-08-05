namespace Usm.Shared.Patterns.Outbox.Abstractions;

/// <summary>
/// Serializes outbox messages for persistence.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface IOutboxSerializer<TMessage>
{
    /// <summary>Serializes a message.</summary>
    byte[] Serialize(TMessage message);

    /// <summary>Deserializes a message.</summary>
    TMessage Deserialize(byte[] payload);
}
