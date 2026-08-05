namespace Usm.Shared.Patterns.Inbox;

/// <summary>
/// Configuration for inbox processing.
/// </summary>
public sealed class InboxOptions
{
    /// <summary>Gets or sets the retention period for processed message keys.</summary>
    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(7);
}
