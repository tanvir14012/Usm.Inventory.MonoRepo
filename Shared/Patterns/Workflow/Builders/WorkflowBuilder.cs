using Usm.Shared.Patterns.Workflow.Abstractions;
using Usm.Shared.Patterns.Workflow.Configuration;

namespace Usm.Shared.Patterns.Workflow.Builders;

/// <summary>
/// Fluent builder for constructing reusable workflows.
/// </summary>
/// <typeparam name="TContext">The workflow context.</typeparam>
public sealed class WorkflowBuilder<TContext> : IWorkflowBuilder<TContext>
{
    private readonly List<IWorkflowStep<TContext>> _steps = [];
    private IWorkflowPersistence<TContext>? _persistence;
    private string? _workflowId;

    /// <inheritdoc />
    public IWorkflowBuilder<TContext> Then(Func<TContext, TContext> step)
    {
        _steps.Add(new DelegateWorkflowStep<TContext>(step));
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder<TContext> ThenAsync(Func<TContext, CancellationToken, ValueTask<TContext>> step)
    {
        _steps.Add(new AsyncDelegateWorkflowStep<TContext>(step));
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder<TContext> When(
        Func<TContext, bool> predicate,
        Action<IWorkflowBuilder<TContext>> thenBranch,
        Action<IWorkflowBuilder<TContext>>? elseBranch = null)
    {
        var thenBuilder = new WorkflowBuilder<TContext>();
        thenBranch(thenBuilder);

        WorkflowBuilder<TContext>? elseBuilder = null;
        if (elseBranch is not null)
        {
            elseBuilder = new WorkflowBuilder<TContext>();
            elseBranch(elseBuilder);
        }

        _steps.Add(new ConditionalWorkflowStep<TContext>(predicate, thenBuilder.Build(), elseBuilder?.Build()));
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder<TContext> Parallel(
        IReadOnlyList<Func<TContext, CancellationToken, ValueTask<TContext>>> branches,
        Func<TContext, IReadOnlyList<TContext>, TContext> combiner)
    {
        _steps.Add(new ParallelWorkflowStep<TContext>(branches, combiner));
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder<TContext> Retry(
        Func<TContext, CancellationToken, ValueTask<TContext>> step,
        RetryOptions? options = null)
    {
        _steps.Add(new RetryWorkflowStep<TContext>(step, options ?? new RetryOptions()));
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder<TContext> Compensate(
        Func<TContext, CancellationToken, ValueTask<TContext>> step,
        Func<TContext, CancellationToken, ValueTask> compensation)
    {
        _steps.Add(new CompensatingWorkflowStep<TContext>(step, compensation));
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder<TContext> WithPersistence(IWorkflowPersistence<TContext> persistence, string workflowId)
    {
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        _workflowId = string.IsNullOrWhiteSpace(workflowId) ? throw new ArgumentException("Workflow id is required.", nameof(workflowId)) : workflowId;
        return this;
    }

    /// <inheritdoc />
    public IWorkflow<TContext> Build()
        => new CompositeWorkflow<TContext>(_steps, _persistence, _workflowId);
}
