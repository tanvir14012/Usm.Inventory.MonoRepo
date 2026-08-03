using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Usm.Shared.Data.Scalability.Abstractions;

namespace Usm.Shared.Data.Scalability.Polling;

/// <summary>
/// Generic EF Core outbox poller that uses <c>SELECT … FOR UPDATE SKIP LOCKED</c>
/// to safely dequeue messages without lock contention between concurrent workers.
/// <para>
/// Requirements:
/// <list type="bullet">
/// <item><typeparamref name="TMessage"/> must implement <see cref="IOutboxMessage"/>.</item>
/// <item>Register via <c>services.AddDbContextFactory&lt;TDbContext&gt;()</c> and
///       <c>services.AddEfCoreOutboxPoller&lt;TDbContext, TMessage&gt;()</c>.</item>
/// </list>
/// </para>
/// </summary>
public sealed class EfCoreOutboxPoller<TDbContext, TMessage>(
    IDbContextFactory<TDbContext> contextFactory,
    IOptions<OutboxPollerOptions> options,
    ILogger<EfCoreOutboxPoller<TDbContext, TMessage>> logger)
    : IOutboxPoller<TMessage>
    where TDbContext : DbContext
    where TMessage : class, IOutboxMessage
{
    private readonly IDbContextFactory<TDbContext> _contextFactory = contextFactory;
    private readonly OutboxPollerOptions _options = options.Value;
    private readonly ILogger<EfCoreOutboxPoller<TDbContext, TMessage>> _logger = logger;

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<TMessage>> PollAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        // SELECT … FOR UPDATE SKIP LOCKED is not translated by LINQ; raw SQL is required.
        var sql = $"""
            SELECT *
            FROM   {_options.Schema}.{_options.TableName}
            WHERE  processed_at IS NULL
              AND  retry_count < @maxRetry
            ORDER  BY created_at ASC
            LIMIT  @batch
            FOR UPDATE SKIP LOCKED
            """;

        var messages = await db.Set<TMessage>()
            .FromSqlRaw(sql,
                new NpgsqlParameter("maxRetry", _options.MaxRetryCount),
                new NpgsqlParameter("batch",    batchSize))
            .AsTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("Outbox poll: {Count} message(s) dequeued.", messages.Count);
        return messages;
    }

    /// <inheritdoc />
    public async ValueTask AcknowledgeAsync(
        IReadOnlyList<TMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0) return;

        await using var db = await _contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        foreach (var msg in messages)
        {
            msg.ProcessedAt = now;
            msg.Error = null;
            db.Entry(msg).State = EntityState.Modified;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Outbox: {Count} message(s) acknowledged.", messages.Count);
    }

    /// <inheritdoc />
    public async ValueTask NackAsync(
        TMessage message,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        message.RetryCount++;
        message.Error = reason;
        db.Entry(message).State = EntityState.Modified;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogWarning(
            "Outbox message {Id} nacked (retry={Retry}/{Max}): {Reason}",
            message.Id, message.RetryCount, _options.MaxRetryCount, reason);
    }
}
