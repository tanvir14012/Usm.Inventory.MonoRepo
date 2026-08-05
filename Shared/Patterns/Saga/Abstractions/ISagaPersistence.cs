namespace Usm.Shared.Patterns.Saga.Abstractions;

/// <summary>
/// Persists saga snapshots for recovery and auditing.
/// </summary>
/// <typeparam name="TContext">The saga context type.</typeparam>
public interface ISagaPersistence<TContext>
{
    /// <summary>Saves a snapshot.</summary>
    ValueTask SaveAsync(SagaSnapshot<TContext> snapshot, CancellationToken cancellationToken = default);

    /// <summary>Loads a snapshot when available.</summary>
    ValueTask<SagaSnapshot<TContext>?> LoadAsync(string sagaId, CancellationToken cancellationToken = default);

    /// <summary>Deletes persisted saga data.</summary>
    ValueTask DeleteAsync(string sagaId, CancellationToken cancellationToken = default);
}
