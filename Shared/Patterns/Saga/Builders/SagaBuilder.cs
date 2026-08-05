using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usm.Shared.Patterns.Saga.Abstractions;
using Usm.Shared.Patterns.Saga.Extensions;

namespace Usm.Shared.Patterns.Saga.Builders;

/// <summary>
/// Fluent builder for saga composition.
/// </summary>
/// <typeparam name="TContext">The saga context type.</typeparam>
public sealed class SagaBuilder<TContext> : ISagaBuilder<TContext>
{
    private readonly List<SagaStepDefinition<TContext>> _steps = new();
    private ISagaPersistence<TContext>? _persistence;
    private ILogger<ISaga<TContext>>? _logger;
    private string? _sagaId;

    /// <inheritdoc />
    public ISagaBuilder<TContext> Use(Func<TContext, CancellationToken, ValueTask<TContext>> step, Func<TContext, CancellationToken, ValueTask>? compensation = null)
    {
        _steps.Add(new SagaStepDefinition<TContext>(step ?? throw new ArgumentNullException(nameof(step)), compensation));
        return this;
    }

    /// <inheritdoc />
    public ISagaBuilder<TContext> WithPersistence(ISagaPersistence<TContext> persistence)
    {
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        return this;
    }

    /// <inheritdoc />
    public ISagaBuilder<TContext> WithLogger(ILogger<ISaga<TContext>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        return this;
    }

    /// <inheritdoc />
    public ISagaBuilder<TContext> WithSagaId(string sagaId)
    {
        _sagaId = string.IsNullOrWhiteSpace(sagaId) ? throw new ArgumentException("Saga id is required.", nameof(sagaId)) : sagaId;
        return this;
    }

    /// <inheritdoc />
    public ISaga<TContext> Build()
        => new Saga<TContext>(
            _steps.ToArray(),
            _persistence ?? new InMemorySagaPersistence<TContext>(),
            _sagaId ?? typeof(TContext).FullName ?? typeof(TContext).Name,
            _logger ?? NullLogger<ISaga<TContext>>.Instance);
}
