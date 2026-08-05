using Usm.Shared.Patterns.Workflow.Abstractions;
using Usm.Shared.Patterns.Workflow.Configuration;

namespace Usm.Shared.Patterns.Workflow;

/// <summary>
/// Base type for reusable workflows.
/// </summary>
/// <typeparam name="TContext">The workflow context.</typeparam>
public abstract class Workflow<TContext> : IWorkflow<TContext>
{
    /// <summary>Creates a builder for constructing a workflow.</summary>
    public static Builders.WorkflowBuilder<TContext> CreateBuilder()
        => new();

    /// <inheritdoc />
    public abstract bool CanExecuteSynchronously { get; }

    /// <inheritdoc />
    public abstract TContext Execute(TContext context);

    /// <inheritdoc />
    public abstract ValueTask<TContext> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);
}

internal interface IWorkflowStep<TContext>
{
    bool CanExecuteSynchronously { get; }

    ValueTask<TContext> ExecuteAsync(
        TContext context,
        WorkflowExecutionState<TContext> state,
        CancellationToken cancellationToken);
}

internal sealed class WorkflowExecutionState<TContext>
{
    private readonly Stack<Func<CancellationToken, ValueTask>> _compensations = new();
    private readonly IWorkflowPersistence<TContext>? _persistence;
    private readonly string? _workflowId;
    private readonly bool _persistSnapshots;

    public WorkflowExecutionState(
        IWorkflowPersistence<TContext>? persistence,
        string? workflowId,
        bool persistSnapshots)
    {
        _persistence = persistence;
        _workflowId = workflowId;
        _persistSnapshots = persistSnapshots;
    }

    public void RegisterCompensation(Func<CancellationToken, ValueTask> compensation)
        => _compensations.Push(compensation);

    public async ValueTask PersistAsync(TContext context, int stepIndex, WorkflowStatus status, CancellationToken cancellationToken)
    {
        if (_persistence is null || !_persistSnapshots || string.IsNullOrWhiteSpace(_workflowId))
            return;

        await _persistence.SaveAsync(
            new WorkflowSnapshot<TContext>(_workflowId, stepIndex, context, status, DateTimeOffset.UtcNow),
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask ExecuteCompensationsAsync(CancellationToken cancellationToken)
    {
        while (_compensations.Count > 0)
            await _compensations.Pop()(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Default reusable workflow implementation.
/// </summary>
/// <typeparam name="TContext">The workflow context.</typeparam>
public sealed class CompositeWorkflow<TContext> : Workflow<TContext>
{
    private readonly IReadOnlyList<IWorkflowStep<TContext>> _steps;
    private readonly IWorkflowPersistence<TContext>? _persistence;
    private readonly string? _workflowId;

    internal CompositeWorkflow(
        IReadOnlyList<IWorkflowStep<TContext>> steps,
        IWorkflowPersistence<TContext>? persistence,
        string? workflowId)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _persistence = persistence;
        _workflowId = workflowId;
    }

    /// <inheritdoc />
    public override bool CanExecuteSynchronously => _steps.All(step => step.CanExecuteSynchronously);

    /// <inheritdoc />
    public override TContext Execute(TContext context)
    {
        if (!CanExecuteSynchronously)
            throw new NotSupportedException("This workflow requires asynchronous execution.");

        return ExecuteAsync(context, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public override async ValueTask<TContext> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        var state = new WorkflowExecutionState<TContext>(_persistence, _workflowId, true);
        var current = context;

        await state.PersistAsync(current, 0, WorkflowStatus.Running, cancellationToken).ConfigureAwait(false);

        try
        {
            for (var index = 0; index < _steps.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                current = await _steps[index].ExecuteAsync(current, state, cancellationToken).ConfigureAwait(false);
                await state.PersistAsync(current, index + 1, WorkflowStatus.Running, cancellationToken).ConfigureAwait(false);
            }

            await state.PersistAsync(current, _steps.Count, WorkflowStatus.Completed, cancellationToken).ConfigureAwait(false);
            if (_persistence is not null && !string.IsNullOrWhiteSpace(_workflowId))
                await _persistence.DeleteAsync(_workflowId, cancellationToken).ConfigureAwait(false);

            return current;
        }
        catch
        {
            await state.PersistAsync(current, _steps.Count, WorkflowStatus.Compensating, cancellationToken).ConfigureAwait(false);
            await state.ExecuteCompensationsAsync(cancellationToken).ConfigureAwait(false);
            await state.PersistAsync(current, _steps.Count, WorkflowStatus.Compensated, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}

internal sealed class DelegateWorkflowStep<TContext> : IWorkflowStep<TContext>
{
    private readonly Func<TContext, TContext> _step;

    public DelegateWorkflowStep(Func<TContext, TContext> step)
        => _step = step ?? throw new ArgumentNullException(nameof(step));

    public bool CanExecuteSynchronously => true;

    public ValueTask<TContext> ExecuteAsync(TContext context, WorkflowExecutionState<TContext> state, CancellationToken cancellationToken)
        => ValueTask.FromResult(_step(context));
}

internal sealed class AsyncDelegateWorkflowStep<TContext> : IWorkflowStep<TContext>
{
    private readonly Func<TContext, CancellationToken, ValueTask<TContext>> _step;

    public AsyncDelegateWorkflowStep(Func<TContext, CancellationToken, ValueTask<TContext>> step)
        => _step = step ?? throw new ArgumentNullException(nameof(step));

    public bool CanExecuteSynchronously => false;

    public ValueTask<TContext> ExecuteAsync(TContext context, WorkflowExecutionState<TContext> state, CancellationToken cancellationToken)
        => _step(context, cancellationToken);
}

internal sealed class ConditionalWorkflowStep<TContext> : IWorkflowStep<TContext>
{
    private readonly Func<TContext, bool> _predicate;
    private readonly IWorkflow<TContext> _thenWorkflow;
    private readonly IWorkflow<TContext>? _elseWorkflow;

    public ConditionalWorkflowStep(Func<TContext, bool> predicate, IWorkflow<TContext> thenWorkflow, IWorkflow<TContext>? elseWorkflow)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _thenWorkflow = thenWorkflow ?? throw new ArgumentNullException(nameof(thenWorkflow));
        _elseWorkflow = elseWorkflow;
    }

    public bool CanExecuteSynchronously => _thenWorkflow.CanExecuteSynchronously && (_elseWorkflow?.CanExecuteSynchronously ?? true);

    public async ValueTask<TContext> ExecuteAsync(TContext context, WorkflowExecutionState<TContext> state, CancellationToken cancellationToken)
        => _predicate(context)
            ? await _thenWorkflow.ExecuteAsync(context, cancellationToken).ConfigureAwait(false)
            : _elseWorkflow is null
                ? context
                : await _elseWorkflow.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
}

internal sealed class ParallelWorkflowStep<TContext> : IWorkflowStep<TContext>
{
    private readonly IReadOnlyList<Func<TContext, CancellationToken, ValueTask<TContext>>> _branches;
    private readonly Func<TContext, IReadOnlyList<TContext>, TContext> _combiner;

    public ParallelWorkflowStep(
        IReadOnlyList<Func<TContext, CancellationToken, ValueTask<TContext>>> branches,
        Func<TContext, IReadOnlyList<TContext>, TContext> combiner)
    {
        _branches = branches ?? throw new ArgumentNullException(nameof(branches));
        _combiner = combiner ?? throw new ArgumentNullException(nameof(combiner));
    }

    public bool CanExecuteSynchronously => false;

    public async ValueTask<TContext> ExecuteAsync(TContext context, WorkflowExecutionState<TContext> state, CancellationToken cancellationToken)
    {
        var tasks = new Task<TContext>[_branches.Count];
        for (var i = 0; i < _branches.Count; i++)
            tasks[i] = _branches[i](context, cancellationToken).AsTask();

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return _combiner(context, results);
    }
}

internal sealed class RetryWorkflowStep<TContext> : IWorkflowStep<TContext>
{
    private readonly Func<TContext, CancellationToken, ValueTask<TContext>> _step;
    private readonly RetryOptions _options;

    public RetryWorkflowStep(Func<TContext, CancellationToken, ValueTask<TContext>> step, RetryOptions options)
    {
        _step = step ?? throw new ArgumentNullException(nameof(step));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool CanExecuteSynchronously => false;

    public async ValueTask<TContext> ExecuteAsync(TContext context, WorkflowExecutionState<TContext> state, CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, _options.MaxAttempts);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await _step(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < attempts)
            {
                lastException = ex;
                var delay = ComputeDelay(attempt);
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? new InvalidOperationException("Retry step failed.");
    }

    private TimeSpan ComputeDelay(int attempt)
    {
        var delay = _options.Strategy switch
        {
            RetryDelayStrategy.Linear => TimeSpan.FromTicks(_options.Delay.Ticks * attempt),
            RetryDelayStrategy.Exponential => TimeSpan.FromTicks(_options.Delay.Ticks * (long)Math.Pow(2, attempt - 1)),
            _ => _options.Delay
        };

        if (!_options.UseJitter || delay <= TimeSpan.Zero)
            return delay;

        var jitterTicks = Math.Max(1, delay.Ticks / 10);
        var offset = Random.Shared.NextInt64(-jitterTicks, jitterTicks + 1);
        var adjusted = delay.Ticks + offset;
        return TimeSpan.FromTicks(Math.Max(TimeSpan.Zero.Ticks, adjusted));
    }
}

internal sealed class CompensatingWorkflowStep<TContext> : IWorkflowStep<TContext>
{
    private readonly Func<TContext, CancellationToken, ValueTask<TContext>> _step;
    private readonly Func<TContext, CancellationToken, ValueTask> _compensation;

    public CompensatingWorkflowStep(
        Func<TContext, CancellationToken, ValueTask<TContext>> step,
        Func<TContext, CancellationToken, ValueTask> compensation)
    {
        _step = step ?? throw new ArgumentNullException(nameof(step));
        _compensation = compensation ?? throw new ArgumentNullException(nameof(compensation));
    }

    public bool CanExecuteSynchronously => false;

    public async ValueTask<TContext> ExecuteAsync(TContext context, WorkflowExecutionState<TContext> state, CancellationToken cancellationToken)
    {
        var next = await _step(context, cancellationToken).ConfigureAwait(false);
        state.RegisterCompensation(token => _compensation(next, token));
        return next;
    }
}
