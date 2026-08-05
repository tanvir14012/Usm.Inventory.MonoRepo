using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.EventBus;
using Usm.Shared.Patterns.EventBus.Abstractions;
using Usm.Shared.Patterns.EventBus.Builders;

namespace Usm.Shared.Patterns.EventBus.Extensions;

/// <summary>
/// Common extension methods for event bus creation and DI registration.
/// </summary>
public static class EventBusExtensions
{
    /// <summary>Registers the event bus framework with dependency injection.</summary>
    public static IServiceCollection AddEventBusFramework(this IServiceCollection services)
    {
        services.AddOptions<EventBusOptions>();
        services.TryAddTransient(typeof(EventBusBuilder<>), typeof(EventBusBuilder<>));
        return services;
    }
}

internal sealed class EventBus<TEvent> : IEventBus<TEvent>
{
    private readonly IReadOnlyList<EventSubscription<TEvent>> _subscriptions;
    private readonly IReadOnlyList<Func<TEvent, Func<CancellationToken, ValueTask>, CancellationToken, ValueTask>> _middlewares;
    private readonly EventBusOptions _options;
    private readonly ILogger<EventBus<TEvent>> _logger;

    public EventBus(
        IReadOnlyList<EventSubscription<TEvent>> subscriptions,
        IReadOnlyList<Func<TEvent, Func<CancellationToken, ValueTask>, CancellationToken, ValueTask>> middlewares,
        EventBusOptions options,
        ILogger<EventBus<TEvent>>? logger = null)
    {
        _subscriptions = subscriptions.OrderByDescending(item => item.Priority).ThenBy(item => item.Sequence).ToArray();
        _middlewares = middlewares;
        _options = options;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<EventBus<TEvent>>.Instance;
    }

    public void Publish(TEvent @event, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var applicable = _subscriptions;
        if (applicable.Count == 0)
        {
            if (_options.ThrowIfNoSubscribers)
                throw new InvalidOperationException("No handlers are subscribed to this event type.");

            return;
        }

        if (_middlewares.Count == 0)
        {
            DispatchSync(@event, applicable, cancellationToken);
            return;
        }

        Action<CancellationToken> terminal = ct => DispatchSync(@event, applicable, ct);
        foreach (var middleware in _middlewares.Reverse())
        {
            var next = terminal;
            terminal = ct => middleware(@event, innerCt =>
            {
                next(innerCt);
                return ValueTask.CompletedTask;
            }, ct).GetAwaiter().GetResult();
        }

        terminal(cancellationToken);
    }

    public async ValueTask PublishAsync(TEvent @event, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var applicable = _subscriptions;
        if (applicable.Count == 0)
        {
            if (_options.ThrowIfNoSubscribers)
                throw new InvalidOperationException("No handlers are subscribed to this event type.");

            return;
        }

        if (_middlewares.Count == 0)
        {
            await DispatchAsync(@event, applicable, cancellationToken).ConfigureAwait(false);
            return;
        }

        Func<CancellationToken, ValueTask> terminal = ct => DispatchAsync(@event, applicable, ct);
        foreach (var middleware in _middlewares.Reverse())
        {
            var next = terminal;
            terminal = ct => middleware(@event, next, ct);
        }

        await terminal(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask DispatchAsync(TEvent @event, IReadOnlyList<EventSubscription<TEvent>> subscriptions, CancellationToken cancellationToken)
    {
        if (_options.DispatchMode == EventDispatchMode.Parallel)
        {
            var tasks = new Task[subscriptions.Count];
            for (var i = 0; i < subscriptions.Count; i++)
            {
                var subscription = subscriptions[i];
                tasks[i] = subscription.Handler is not null
                    ? Task.Run(() => subscription.Handler(@event), cancellationToken)
                    : subscription.AsyncHandler!(@event, cancellationToken).AsTask();
            }

            if (_options.FailFast)
                await Task.WhenAll(tasks).ConfigureAwait(false);
            else
            {
                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "One or more event handlers failed.");
                    throw;
                }
            }

            return;
        }

        foreach (var subscription in subscriptions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await subscription.InvokeAsync(@event, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Event handler failed.");
                if (_options.FailFast)
                    throw;
            }
        }
    }

    private void DispatchSync(TEvent @event, IReadOnlyList<EventSubscription<TEvent>> subscriptions, CancellationToken cancellationToken)
    {
        if (_options.DispatchMode == EventDispatchMode.Parallel)
        {
            var tasks = new Task[subscriptions.Count];
            for (var i = 0; i < subscriptions.Count; i++)
                tasks[i] = subscriptions[i].InvokeAsync(@event, cancellationToken).AsTask();

            try
            {
                Task.WhenAll(tasks).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "One or more event handlers failed.");
                if (_options.FailFast)
                    throw;
            }

            return;
        }

        foreach (var subscription in subscriptions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (subscription.Handler is not null)
                {
                    subscription.Handler(@event);
                    continue;
                }

                subscription.AsyncHandler!(@event, cancellationToken).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Event handler failed.");
                if (_options.FailFast)
                    throw;
            }
        }
    }
}

internal sealed record EventSubscription<TEvent>
{
    private EventSubscription(
        long sequence,
        int priority,
        string? group,
        Action<TEvent>? handler,
        Func<TEvent, CancellationToken, ValueTask>? asyncHandler)
    {
        Sequence = sequence;
        Priority = priority;
        Group = group;
        Handler = handler;
        AsyncHandler = asyncHandler;
    }

    private static long _sequence;

    public long Sequence { get; }
    public int Priority { get; }
    public string? Group { get; }
    public Action<TEvent>? Handler { get; }
    public Func<TEvent, CancellationToken, ValueTask>? AsyncHandler { get; }

    public static EventSubscription<TEvent> FromHandler(Action<TEvent> handler, int priority, string? group)
        => new(Interlocked.Increment(ref _sequence), priority, group, handler, null);

    public static EventSubscription<TEvent> FromAsyncHandler(Func<TEvent, CancellationToken, ValueTask> handler, int priority, string? group)
        => new(Interlocked.Increment(ref _sequence), priority, group, null, handler);

    public ValueTask InvokeAsync(TEvent @event, CancellationToken cancellationToken)
    {
        if (Handler is not null)
        {
            Handler(@event);
            return ValueTask.CompletedTask;
        }

        return AsyncHandler!(@event, cancellationToken);
    }
}
