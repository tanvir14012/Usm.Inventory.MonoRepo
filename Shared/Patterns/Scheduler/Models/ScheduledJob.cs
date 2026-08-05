using Usm.Shared.Patterns.Scheduler.Abstractions;

namespace Usm.Shared.Patterns.Scheduler;

/// <summary>
/// Represents a scheduled job instance.
/// </summary>
/// <typeparam name="TJob">The job payload type.</typeparam>
public sealed record ScheduledJob<TJob>(
    Guid Id,
    TJob Job,
    ISchedule Schedule,
    int Priority,
    DateTimeOffset DueAt,
    int Attempts,
    int MaxAttempts);
