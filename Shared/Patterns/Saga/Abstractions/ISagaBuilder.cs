namespace Usm.Shared.Patterns.Saga.Abstractions;

/// <summary>
/// Fluent builder for saga orchestration.
/// </summary>
/// <typeparam name="TContext">The saga context type.</typeparam>
public interface ISagaBuilder<TContext>
{
    /// <summary>Adds a saga step with an optional compensation action.</summary>
    ISagaBuilder<TContext> Use(Func<TContext, CancellationToken, ValueTask<TContext>> step, Func<TContext, CancellationToken, ValueTask>? compensation = null);

    /// <summary>Sets the persistence abstraction.</summary>
    ISagaBuilder<TContext> WithPersistence(ISagaPersistence<TContext> persistence);

    /// <summary>Sets the logger.</summary>
    ISagaBuilder<TContext> WithLogger(Microsoft.Extensions.Logging.ILogger<ISaga<TContext>> logger);

    /// <summary>Sets the saga identifier.</summary>
    ISagaBuilder<TContext> WithSagaId(string sagaId);

    /// <summary>Builds the saga.</summary>
    ISaga<TContext> Build();
}
