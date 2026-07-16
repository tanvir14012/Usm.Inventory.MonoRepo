namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Models;

/// <summary>
/// Persistent record representing a single background job (Outbox pattern).
/// </summary>
public sealed class JobRecord
{
    public Guid   JobId              { get; set; } = Guid.NewGuid();
    public string JobType            { get; set; } = string.Empty;   // AssemblyQualifiedName
    public string? SerializedPayload { get; set; }
    public int    Priority           { get; set; }
    public JobStatus Status          { get; set; } = JobStatus.Pending;
    public int    RetryCount         { get; set; }
    public string? IdempotencyKey    { get; set; }

    public DateTimeOffset  CreatedAt    { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt    { get; set; }
    public DateTimeOffset? CompletedAt  { get; set; }
    public string?         ErrorMessage { get; set; }
}
