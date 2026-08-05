namespace Usm.Shared.Patterns.Workflow.Configuration;

/// <summary>
/// Configuration for workflow execution and persistence.
/// </summary>
public sealed class WorkflowOptions
{
    /// <summary>Gets or sets a value indicating whether workflow snapshots should be persisted while running.</summary>
    public bool PersistSnapshots { get; set; } = true;
}
