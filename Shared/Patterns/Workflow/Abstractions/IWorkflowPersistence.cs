namespace Usm.Shared.Patterns.Workflow.Abstractions;

/// <summary>
/// Persists workflow snapshots for saga-style recovery and replay.
/// </summary>
/// <typeparam name="TContext">The workflow context.</typeparam>
public interface IWorkflowPersistence<TContext>
{
    /// <summary>Saves a workflow snapshot.</summary>
    ValueTask SaveAsync(WorkflowSnapshot<TContext> snapshot, CancellationToken cancellationToken = default);

    /// <summary>Loads a workflow snapshot when available.</summary>
    ValueTask<WorkflowSnapshot<TContext>?> LoadAsync(string workflowId, CancellationToken cancellationToken = default);

    /// <summary>Deletes persisted workflow state.</summary>
    ValueTask DeleteAsync(string workflowId, CancellationToken cancellationToken = default);
}
