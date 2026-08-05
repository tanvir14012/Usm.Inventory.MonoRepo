namespace Usm.Shared.Patterns.Saga;

/// <summary>
/// Configuration for saga execution.
/// </summary>
public sealed class SagaOptions
{
    /// <summary>Gets or sets a value indicating whether snapshots should be persisted while running.</summary>
    public bool PersistSnapshots { get; set; } = true;
}
