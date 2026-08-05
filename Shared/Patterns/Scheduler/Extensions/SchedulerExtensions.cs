using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.Scheduler.Abstractions;
using Usm.Shared.Patterns.Scheduler.Builders;

namespace Usm.Shared.Patterns.Scheduler.Extensions;

/// <summary>
/// Common extension methods for scheduler registration.
/// </summary>
public static class SchedulerExtensions
{
    /// <summary>Registers the scheduler framework with dependency injection.</summary>
    public static IServiceCollection AddSchedulerFramework(this IServiceCollection services)
    {
        services.AddOptions<SchedulerOptions>();
        services.TryAddTransient(typeof(ISchedulerBuilder<>), typeof(SchedulerBuilder<>));
        services.TryAddSingleton(typeof(IJobStore<>), typeof(InMemoryJobStore<>));
        return services;
    }
}

internal sealed class JobScheduler<TJob> : IJobScheduler<TJob>
{
    private readonly IJobStore<TJob> _store;
    private readonly IJobHandler<TJob> _handler;
    private readonly TimeProvider _timeProvider;
    private readonly SchedulerOptions _options;
    private readonly ILogger<IJobScheduler<TJob>> _logger;

    public JobScheduler(
        IJobStore<TJob> store,
        IJobHandler<TJob> handler,
        TimeProvider timeProvider,
        SchedulerOptions options,
        ILogger<IJobScheduler<TJob>> logger)
    {
        _store = store;
        _handler = handler;
        _timeProvider = timeProvider;
        _options = options;
        _logger = logger;
    }

    public int PendingCount => _store.Count;

    public ValueTask ScheduleAsync(TJob job, ISchedule schedule, int priority = 0, int maxAttempts = 1, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dueAt = schedule.GetNextRun(_timeProvider.GetUtcNow());
        if (dueAt is null)
            throw new InvalidOperationException("Schedule does not produce a next run.");

        return _store.EnqueueAsync(new ScheduledJob<TJob>(Guid.NewGuid(), job, schedule, priority, dueAt.Value, 0, maxAttempts > 0 ? maxAttempts : throw new ArgumentOutOfRangeException(nameof(maxAttempts))), cancellationToken);
    }

    public ValueTask ScheduleDelayedAsync(TJob job, TimeSpan delay, int priority = 0, int maxAttempts = 1, CancellationToken cancellationToken = default)
        => ScheduleAsync(job, new DelaySchedule(delay), priority, maxAttempts, cancellationToken);

    public ValueTask ScheduleRecurringAsync(TJob job, TimeSpan interval, int priority = 0, int maxAttempts = 1, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var schedule = new IntervalSchedule(interval);
        var dueAt = _timeProvider.GetUtcNow();
        return _store.EnqueueAsync(new ScheduledJob<TJob>(Guid.NewGuid(), job, schedule, priority, dueAt, 0, maxAttempts > 0 ? maxAttempts : throw new ArgumentOutOfRangeException(nameof(maxAttempts))), cancellationToken);
    }

    public ValueTask ScheduleCronAsync(TJob job, CronSchedule schedule, int priority = 0, int maxAttempts = 1, CancellationToken cancellationToken = default)
        => ScheduleAsync(job, schedule, priority, maxAttempts, cancellationToken);

    public async ValueTask<int> RunDueAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var dueJobs = await _store.DequeueDueAsync(utcNow, _options.BatchSize, cancellationToken).ConfigureAwait(false);
        var processed = 0;
        List<Exception>? exceptions = null;

        foreach (var job in dueJobs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _handler.HandleAsync(job.Job, cancellationToken).ConfigureAwait(false);
                processed++;
                _logger.LogDebug("Executed scheduled job {JobId} with priority {Priority}.", job.Id, job.Priority);

                if (job.Schedule.IsRecurring)
                {
                    var nextRun = job.Schedule.GetNextRun(job.DueAt);
                    if (nextRun is not null)
                        await _store.EnqueueAsync(job with { Id = Guid.NewGuid(), DueAt = nextRun.Value, Attempts = 0 }, cancellationToken).ConfigureAwait(false);
                }
            }
            catch when (job.Attempts + 1 < job.MaxAttempts)
            {
                var retryDelay = _options.RetryDelayStrategy(job.Attempts + 1);
                await _store.EnqueueAsync(job with { Id = Guid.NewGuid(), DueAt = utcNow.Add(retryDelay), Attempts = job.Attempts + 1 }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                (exceptions ??= new List<Exception>()).Add(ex);
            }
        }

        if (exceptions is { Count: > 0 })
            throw new AggregateException(exceptions);

        return processed;
    }
}

/// <summary>
/// Default in-memory job store used for tests and local development.
/// </summary>
/// <typeparam name="TJob">The job payload type.</typeparam>
public sealed class InMemoryJobStore<TJob> : IJobStore<TJob>
{
    private readonly PriorityQueue<ScheduledJob<TJob>, JobQueueItem> _queue = new(new JobQueueComparer());
    private readonly object _gate = new();

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_gate)
                return _queue.Count;
        }
    }

    /// <inheritdoc />
    public ValueTask EnqueueAsync(ScheduledJob<TJob> job, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _queue.Enqueue(job, new JobQueueItem(job.DueAt, job.Priority));
            return ValueTask.CompletedTask;
        }
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<ScheduledJob<TJob>>> DequeueDueAsync(DateTimeOffset utcNow, int maxCount, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var jobs = new List<ScheduledJob<TJob>>(Math.Min(maxCount, _queue.Count));
            while (jobs.Count < maxCount && _queue.TryPeek(out var next, out var key) && next.DueAt <= utcNow)
                jobs.Add(_queue.Dequeue());

            return ValueTask.FromResult((IReadOnlyList<ScheduledJob<TJob>>)jobs);
        }
    }

    private readonly record struct JobQueueItem(DateTimeOffset DueAt, int Priority);

    private sealed class JobQueueComparer : IComparer<JobQueueItem>
    {
        public int Compare(JobQueueItem x, JobQueueItem y)
        {
            var due = x.DueAt.CompareTo(y.DueAt);
            if (due != 0)
                return due;

            return y.Priority.CompareTo(x.Priority);
        }
    }
}
