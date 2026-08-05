using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.Outbox;
using Usm.Shared.Patterns.Outbox.Abstractions;
using Usm.Shared.Patterns.Outbox.Builders;
using Usm.Shared.Patterns.Outbox.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.Outbox.Tests;

public sealed class OutboxTests
{
    [Fact]
    public async Task EnqueuesAndDispatchesMessages()
    {
        var dispatched = new List<string>();
        var outbox = new OutboxBuilder<string>()
            .WithDispatcher(new DelegateDispatcher<string>(msg => dispatched.Add(msg)))
            .Build();

        await outbox.EnqueueAsync("hello");
        var count = await outbox.DispatchPendingAsync();

        Assert.Equal(1, count);
        Assert.Equal(new[] { "hello" }, dispatched);
    }

    [Fact]
    public async Task RequeuesFailedMessages()
    {
        var attempts = 0;
        var store = new InMemoryOutboxStore<string>();
        var outbox = new OutboxBuilder<string>()
            .WithStore(store)
            .WithDispatcher(new DelegateDispatcher<string>(_ =>
            {
                attempts++;
                throw new InvalidOperationException("boom");
            }))
            .Build();

        await outbox.EnqueueAsync("hello");
        await Assert.ThrowsAsync<InvalidOperationException>(() => outbox.DispatchPendingAsync().AsTask());

        Assert.Equal(1, attempts);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void SerializesAndDeserializesMessages()
    {
        var serializer = new SystemTextJsonOutboxSerializer<SampleMessage>();
        var message = new SampleMessage("x", 1);

        var payload = serializer.Serialize(message);
        var roundTrip = serializer.Deserialize(payload);

        Assert.Equal(message, roundTrip);
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddOutboxFramework();

        using var provider = services.BuildServiceProvider();
        var builder = provider.GetRequiredService<OutboxBuilder<string>>();

        Assert.NotNull(builder);
    }

    private sealed class DelegateDispatcher<T> : IOutboxDispatcher<T>
    {
        private readonly Action<T> _dispatch;

        public DelegateDispatcher(Action<T> dispatch)
        {
            _dispatch = dispatch;
        }

        public ValueTask DispatchAsync(T message, CancellationToken cancellationToken = default)
        {
            _dispatch(message);
            return ValueTask.CompletedTask;
        }
    }

    private sealed record SampleMessage(string Id, int Version);
}
