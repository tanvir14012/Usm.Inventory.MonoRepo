using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usm.Shared.Patterns.Scheduler.Abstractions;
using Usm.Shared.Patterns.Scheduler.Extensions;

namespace Usm.Shared.Patterns.Scheduler.Builders;

/// <summary>
/// Fluent builder for scheduler configuration.
/// </summary>
/// <typeparam name="TJob">The job payload type.</typeparam>
public sealed class SchedulerBuilder<TJob> : ISchedulerBuilder<TJob>
{
    private IJobHandler<TJob>? _handler;
    private IJobStore<TJob>? _store;
    private TimeProvider? _timeProvider;
    private ILogger<IJobScheduler<TJob>>? _logger;
    private readonly SchedulerOptions _options = new();

    /// <inheritdoc />
    public ISchedulerBuilder<TJob> WithHandler(IJobHandler<TJob> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }

    /// <inheritdoc />
    public ISchedulerBuilder<TJob> WithStore(IJobStore<TJob> store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        return this;
    }

    /// <inheritdoc />
    public ISchedulerBuilder<TJob> WithTimeProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        return this;
    }

    /// <inheritdoc />
    public ISchedulerBuilder<TJob> WithLogger(ILogger<IJobScheduler<TJob>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        return this;
    }

    /// <inheritdoc />
    public ISchedulerBuilder<TJob> WithBatchSize(int batchSize)
    {
        _options.BatchSize = batchSize > 0 ? batchSize : throw new ArgumentOutOfRangeException(nameof(batchSize));
        return this;
    }

    /// <inheritdoc />
    public ISchedulerBuilder<TJob> WithRetryDelayStrategy(Func<int, TimeSpan> retryDelayStrategy)
    {
        _options.RetryDelayStrategy = retryDelayStrategy ?? throw new ArgumentNullException(nameof(retryDelayStrategy));
        return this;
    }

    /// <inheritdoc />
    public IJobScheduler<TJob> Build()
        => new JobScheduler<TJob>(
            _store ?? new InMemoryJobStore<TJob>(),
            _handler ?? throw new InvalidOperationException("A job handler is required."),
            _timeProvider ?? _options.TimeProvider,
            _options,
            _logger ?? NullLogger<IJobScheduler<TJob>>.Instance);
}
