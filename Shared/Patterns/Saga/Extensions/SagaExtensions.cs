using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.Saga.Abstractions;
using Usm.Shared.Patterns.Saga.Builders;

namespace Usm.Shared.Patterns.Saga.Extensions;

/// <summary>
/// Common extension methods for saga registration.
/// </summary>
public static class SagaExtensions
{
    /// <summary>Registers the saga framework with dependency injection.</summary>
    public static IServiceCollection AddSagaFramework(this IServiceCollection services)
    {
        services.AddOptions<SagaOptions>();
        services.TryAddSingleton(typeof(ISagaPersistence<>), typeof(InMemorySagaPersistence<>));
        services.TryAddTransient(typeof(ISagaBuilder<>), typeof(SagaBuilder<>));
        return services;
    }
}

internal sealed class Saga<TContext> : ISaga<TContext>
{
    private readonly SagaStepDefinition<TContext>[] _steps;
    private readonly ISagaPersistence<TContext> _persistence;
    private readonly string _sagaId;
    private readonly Microsoft.Extensions.Logging.ILogger<ISaga<TContext>> _logger;

    public Saga(
        SagaStepDefinition<TContext>[] steps,
        ISagaPersistence<TContext> persistence,
        string sagaId,
        Microsoft.Extensions.Logging.ILogger<ISaga<TContext>> logger)
    {
        _steps = steps;
        _persistence = persistence;
        _sagaId = sagaId;
        _logger = logger;
    }

    public async ValueTask<SagaExecutionResult<TContext>> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        var snapshotsEnabled = true;
        var executedCompensations = new Stack<Func<CancellationToken, ValueTask>>();
        var current = context;

        await PersistAsync(current, 0, SagaStatus.Running, cancellationToken).ConfigureAwait(false);

        try
        {
            for (var i = 0; i < _steps.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                current = await _steps[i].ExecuteAsync(current, cancellationToken).ConfigureAwait(false);
                if (_steps[i].Compensation is not null)
                {
                    var compensation = _steps[i].Compensation!;
                    var compensatedContext = current;
                    executedCompensations.Push(token => compensation(compensatedContext, token));
                }

                if (snapshotsEnabled)
                    await PersistAsync(current, i + 1, SagaStatus.Running, cancellationToken).ConfigureAwait(false);
            }

            await PersistAsync(current, _steps.Length, SagaStatus.Completed, cancellationToken).ConfigureAwait(false);
            await _persistence.DeleteAsync(_sagaId, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Saga {SagaId} completed successfully.", _sagaId);
            return new SagaExecutionResult<TContext>(_sagaId, current, true);
        }
        catch (Exception ex)
        {
            await PersistAsync(current, _steps.Length, SagaStatus.Compensating, cancellationToken).ConfigureAwait(false);

            while (executedCompensations.Count > 0)
                await executedCompensations.Pop()(cancellationToken).ConfigureAwait(false);

            await PersistAsync(current, _steps.Length, SagaStatus.Compensated, cancellationToken).ConfigureAwait(false);
            _logger.LogError(ex, "Saga {SagaId} failed.", _sagaId);
            return new SagaExecutionResult<TContext>(_sagaId, current, false, ex.Message);
        }
    }

    private async ValueTask PersistAsync(TContext context, int stepIndex, SagaStatus status, CancellationToken cancellationToken)
    {
        await _persistence.SaveAsync(
            new SagaSnapshot<TContext>(_sagaId, stepIndex, context, status, DateTimeOffset.UtcNow),
            cancellationToken).ConfigureAwait(false);
    }
}

internal sealed record SagaStepDefinition<TContext>(
    Func<TContext, CancellationToken, ValueTask<TContext>> ExecuteAsync,
    Func<TContext, CancellationToken, ValueTask>? Compensation = null);

/// <summary>
/// Default in-memory saga persistence used for tests and local development.
/// </summary>
/// <typeparam name="TContext">The saga context type.</typeparam>
public sealed class InMemorySagaPersistence<TContext> : ISagaPersistence<TContext>
{
    private readonly List<SagaSnapshot<TContext>> _snapshots = new();
    private readonly object _gate = new();

    /// <summary>Gets the captured snapshots.</summary>
    public IReadOnlyList<SagaSnapshot<TContext>> Snapshots
    {
        get
        {
            lock (_gate)
                return _snapshots.ToArray();
        }
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(SagaSnapshot<TContext> snapshot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
            _snapshots.Add(snapshot);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<SagaSnapshot<TContext>?> LoadAsync(string sagaId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
            return ValueTask.FromResult<SagaSnapshot<TContext>?>(_snapshots.LastOrDefault(snapshot => snapshot.SagaId == sagaId));
    }

    /// <inheritdoc />
    public ValueTask DeleteAsync(string sagaId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
            _snapshots.RemoveAll(snapshot => snapshot.SagaId == sagaId);

        return ValueTask.CompletedTask;
    }
}
