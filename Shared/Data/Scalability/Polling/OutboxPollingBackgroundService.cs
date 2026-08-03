using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Data.Scalability.Abstractions;

namespace Usm.Shared.Data.Scalability.Polling;

/// <summary>
/// Abstract <see cref="BackgroundService"/> that drives the outbox polling loop.
/// Subclass this, override <see cref="ProcessAsync"/>, and register the concrete type
/// via <c>services.AddHostedService&lt;YourPoller&gt;()</c>.
/// <para>
/// Features:
/// <list type="bullet">
/// <item>Configurable batch size and polling interval.</item>
/// <item>Per-message error isolation — one failure does not skip remaining batch messages.</item>
/// <item>Thread-safe batch acknowledgment after all messages in a cycle succeed.</item>
/// <item>Exponential back-off (capped at <see cref="OutboxPollerOptions.MaxBackoffDelay"/>) on repeated poll errors.</item>
/// <item>Graceful shutdown respects the <see cref="CancellationToken"/> from <see cref="IHostApplicationLifetime"/>.</item>
/// </list>
/// </para>
/// </summary>
public abstract class OutboxPollingBackgroundService<TMessage>(
    IOutboxPoller<TMessage> poller,
    IOptions<OutboxPollerOptions> options,
    ILogger logger)
    : BackgroundService
    where TMessage : class
{
    private readonly IOutboxPoller<TMessage> _poller = poller;
    private readonly OutboxPollerOptions _options = options.Value;
    private readonly ILogger _logger = logger;
    private int _consecutiveFailures;

    /// <summary>
    /// Processes a single dequeued message.
    /// Throw any exception to trigger a nack and schedule a retry.
    /// </summary>
    protected abstract ValueTask ProcessAsync(TMessage message, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Outbox polling starting for {Message}. Initial delay: {Delay}.",
            typeof(TMessage).Name, _options.InitialDelay);

        await DelayAsync(_options.InitialDelay, stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await _poller
                    .PollAsync(_options.BatchSize, stoppingToken)
                    .ConfigureAwait(false);

                if (messages.Count == 0)
                {
                    _consecutiveFailures = 0;
                    await DelayAsync(_options.PollingInterval, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                var succeeded = new List<TMessage>(messages.Count);

                foreach (var message in messages)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    try
                    {
                        await ProcessAsync(message, stoppingToken).ConfigureAwait(false);
                        succeeded.Add(message);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to process outbox message {Id}.",
                            GetId(message));
                        await _poller.NackAsync(message, ex.Message, stoppingToken)
                            .ConfigureAwait(false);
                    }
                }

                if (succeeded.Count > 0)
                    await _poller.AcknowledgeAsync(succeeded, stoppingToken).ConfigureAwait(false);

                _consecutiveFailures = 0;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                var backoff = CalculateBackoff(_consecutiveFailures, _options.MaxBackoffDelay);
                _logger.LogError(ex,
                    "Outbox polling error #{Count}. Back-off: {Backoff}.",
                    _consecutiveFailures, backoff);
                await DelayAsync(backoff, stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Outbox polling stopped for {Message}.", typeof(TMessage).Name);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TimeSpan CalculateBackoff(int failures, TimeSpan maxDelay)
    {
        // 2^n seconds, capped at maxDelay.
        var seconds = Math.Pow(2, Math.Min(failures, 10)); // cap exponent at 1024s
        return TimeSpan.FromSeconds(Math.Min(seconds, maxDelay.TotalSeconds));
    }

    private static async ValueTask DelayAsync(TimeSpan delay, CancellationToken ct)
    {
        try
        { await Task.Delay(delay, ct).ConfigureAwait(false); }
        catch (OperationCanceledException) { /* expected on shutdown */ }
    }

    private static string GetId(TMessage message) =>
        message is IOutboxMessage outbox ? outbox.Id.ToString() : message.GetHashCode().ToString();
}
