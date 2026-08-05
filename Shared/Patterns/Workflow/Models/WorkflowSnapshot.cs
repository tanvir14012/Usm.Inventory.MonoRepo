namespace Usm.Shared.Patterns.Workflow;

/// <summary>
/// Workflow execution status for persistence.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>The workflow is running.</summary>
    Running = 0,

    /// <summary>The workflow completed successfully.</summary>
    Completed = 1,

    /// <summary>The workflow failed.</summary>
    Failed = 2,

    /// <summary>The workflow is compensating after a failure.</summary>
    Compensating = 3,

    /// <summary>The workflow completed compensation.</summary>
    Compensated = 4
}

/// <summary>
/// Snapshot persisted during workflow execution.
/// </summary>
/// <typeparam name="TContext">The workflow context.</typeparam>
public sealed record WorkflowSnapshot<TContext>(
    string WorkflowId,
    int StepIndex,
    TContext Context,
    WorkflowStatus Status,
    DateTimeOffset Timestamp);
