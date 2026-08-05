namespace Usm.Shared.Patterns.Scheduler.Abstractions;

/// <summary>
/// Fluent builder for scheduler configuration.
/// </summary>
/// <typeparam name="TJob">The job payload type.</typeparam>
public interface ISchedulerBuilder<TJob>
{
    /// <summary>Sets the handler.</summary>
    ISchedulerBuilder<TJob> WithHandler(IJobHandler<TJob> handler);

    /// <summary>Sets the backing store.</summary>
    ISchedulerBuilder<TJob> WithStore(IJobStore<TJob> store);

    /// <summary>Sets the time provider.</summary>
    ISchedulerBuilder<TJob> WithTimeProvider(TimeProvider timeProvider);

    /// <summary>Sets the logger.</summary>
    ISchedulerBuilder<TJob> WithLogger(Microsoft.Extensions.Logging.ILogger<IJobScheduler<TJob>> logger);

    /// <summary>Sets the batch size.</summary>
    ISchedulerBuilder<TJob> WithBatchSize(int batchSize);

    /// <summary>Sets the retry delay strategy.</summary>
    ISchedulerBuilder<TJob> WithRetryDelayStrategy(Func<int, TimeSpan> retryDelayStrategy);

    /// <summary>Builds the scheduler.</summary>
    IJobScheduler<TJob> Build();
}
