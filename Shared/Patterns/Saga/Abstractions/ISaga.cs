namespace Usm.Shared.Patterns.Saga.Abstractions;

/// <summary>
/// Executes a saga with compensation support.
/// </summary>
/// <typeparam name="TContext">The saga context type.</typeparam>
public interface ISaga<TContext>
{
    /// <summary>Executes the saga.</summary>
    ValueTask<SagaExecutionResult<TContext>> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);
}
