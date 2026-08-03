namespace Usm.Shared.Data.Scalability.Polling;

public sealed class OutboxPollerOptions
{
    public const string SectionName = "Database:OutboxPoller";

    /// <summary>Schema owning the outbox table.</summary>
    public string Schema { get; set; } = "public";

    /// <summary>Outbox table name (without schema prefix).</summary>
    public string TableName { get; set; } = "outbox_messages";

    /// <summary>Maximum messages dequeued per polling cycle.</summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>How long to wait between polling cycles when the queue is empty.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Initial delay before the first poll, allowing the host to fully initialise.</summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Maximum number of delivery attempts before a message is considered dead-lettered.</summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>Upper bound for exponential back-off on consecutive polling errors.</summary>
    public TimeSpan MaxBackoffDelay { get; set; } = TimeSpan.FromMinutes(5);
}
