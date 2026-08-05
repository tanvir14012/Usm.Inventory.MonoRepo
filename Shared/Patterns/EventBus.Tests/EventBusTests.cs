using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.EventBus;
using Usm.Shared.Patterns.EventBus.Abstractions;
using Usm.Shared.Patterns.EventBus.Builders;
using Usm.Shared.Patterns.EventBus.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.EventBus.Tests;

public sealed class EventBusTests
{
    [Fact]
    public async Task PublishesHandlersByPriority()
    {
        var trace = new List<string>();
        var bus = new EventBusBuilder<string>()
            .SubscribeAsync((evt, token) =>
            {
                trace.Add($"low:{evt}");
                return ValueTask.CompletedTask;
            }, priority: 1)
            .SubscribeAsync((evt, token) =>
            {
                trace.Add($"high:{evt}");
                return ValueTask.CompletedTask;
            }, priority: 10)
            .Build();

        await bus.PublishAsync("evt");

        Assert.Equal(new[] { "high:evt", "low:evt" }, trace);
    }

    [Fact]
    public async Task SupportsParallelDispatch()
    {
        var count = 0;
        var bus = new EventBusBuilder<string>()
            .WithDispatchMode(EventDispatchMode.Parallel)
            .SubscribeAsync(async (_, token) =>
            {
                await Task.Delay(1, token);
                Interlocked.Increment(ref count);
            })
            .SubscribeAsync(async (_, token) =>
            {
                await Task.Delay(1, token);
                Interlocked.Increment(ref count);
            })
            .Build();

        await bus.PublishAsync("evt");

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task RunsMiddlewareAroundDispatch()
    {
        var trace = new List<string>();
        var bus = new EventBusBuilder<string>()
            .Use((evt, next, token) =>
            {
                trace.Add("before");
                return ContinueAsync(evt, next, token, trace);
            })
            .SubscribeAsync((evt, token) =>
            {
                trace.Add("handler");
                return ValueTask.CompletedTask;
            })
            .Build();

        await bus.PublishAsync("evt");

        Assert.Equal(new[] { "before", "handler", "after" }, trace);
    }

    [Fact]
    public async Task ThrowsWhenNoSubscribersConfigured()
    {
        var bus = new EventBusBuilder<string>().Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync("evt").AsTask());
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddEventBusFramework();

        using var provider = services.BuildServiceProvider();
        var builder = provider.GetRequiredService<EventBusBuilder<string>>();

        Assert.NotNull(builder);
    }

    private static async ValueTask ContinueAsync(string evt, Func<CancellationToken, ValueTask> next, CancellationToken token, List<string> trace)
    {
        await next(token);
        trace.Add("after");
    }
}
