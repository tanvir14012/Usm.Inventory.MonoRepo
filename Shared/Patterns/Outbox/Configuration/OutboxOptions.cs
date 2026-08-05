namespace Usm.Shared.Patterns.Outbox;

/// <summary>
/// Configuration for outbox processing.
/// </summary>
public sealed class OutboxOptions
{
    /// <summary>Gets or sets the batch size used during dispatch.</summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>Gets or sets the retention period for pending records.</summary>
    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(7);
}
