namespace Usm.Shared.Patterns.Saga;

/// <summary>
/// Saga status for persistence.
/// </summary>
public enum SagaStatus
{
    /// <summary>The saga is running.</summary>
    Running = 0,

    /// <summary>The saga completed successfully.</summary>
    Completed = 1,

    /// <summary>The saga failed.</summary>
    Failed = 2,

    /// <summary>The saga is compensating after failure.</summary>
    Compensating = 3,

    /// <summary>The saga completed compensation.</summary>
    Compensated = 4
}

/// <summary>
/// Persisted saga snapshot.
/// </summary>
/// <typeparam name="TContext">The saga context type.</typeparam>
public sealed record SagaSnapshot<TContext>(
    string SagaId,
    int StepIndex,
    TContext Context,
    SagaStatus Status,
    DateTimeOffset Timestamp);

/// <summary>
/// Result of executing a saga.
/// </summary>
/// <typeparam name="TContext">The saga context type.</typeparam>
public sealed record SagaExecutionResult<TContext>(
    string SagaId,
    TContext Context,
    bool Succeeded,
    string? Error = null);
