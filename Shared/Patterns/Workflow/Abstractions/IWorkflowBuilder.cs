using Usm.Shared.Patterns.Workflow.Configuration;

namespace Usm.Shared.Patterns.Workflow.Abstractions;

/// <summary>
/// Fluent builder for a reusable workflow.
/// </summary>
/// <typeparam name="TContext">The workflow context.</typeparam>
public interface IWorkflowBuilder<TContext>
{
    /// <summary>Adds a synchronous step.</summary>
    IWorkflowBuilder<TContext> Then(Func<TContext, TContext> step);

    /// <summary>Adds an asynchronous step.</summary>
    IWorkflowBuilder<TContext> ThenAsync(Func<TContext, CancellationToken, ValueTask<TContext>> step);

    /// <summary>Adds a conditional branch.</summary>
    IWorkflowBuilder<TContext> When(
        Func<TContext, bool> predicate,
        Action<IWorkflowBuilder<TContext>> thenBranch,
        Action<IWorkflowBuilder<TContext>>? elseBranch = null);

    /// <summary>Adds a parallel branch set.</summary>
    IWorkflowBuilder<TContext> Parallel(
        IReadOnlyList<Func<TContext, CancellationToken, ValueTask<TContext>>> branches,
        Func<TContext, IReadOnlyList<TContext>, TContext> combiner);

    /// <summary>Adds a retrying step.</summary>
    IWorkflowBuilder<TContext> Retry(
        Func<TContext, CancellationToken, ValueTask<TContext>> step,
        RetryOptions? options = null);

    /// <summary>Adds a compensating step.</summary>
    IWorkflowBuilder<TContext> Compensate(
        Func<TContext, CancellationToken, ValueTask<TContext>> step,
        Func<TContext, CancellationToken, ValueTask> compensation);

    /// <summary>Associates persistence with the workflow.</summary>
    IWorkflowBuilder<TContext> WithPersistence(IWorkflowPersistence<TContext> persistence, string workflowId);

    /// <summary>Builds the workflow.</summary>
    IWorkflow<TContext> Build();
}
