using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.Inbox;
using Usm.Shared.Patterns.Inbox.Abstractions;
using Usm.Shared.Patterns.Inbox.Builders;
using Usm.Shared.Patterns.Inbox.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.Inbox.Tests;

public sealed class InboxTests
{
    [Fact]
    public async Task ProcessesMessageOnce()
    {
        var handled = 0;
        var inbox = new InboxBuilder<SampleMessage, string>()
            .WithKeySelector(message => message.Id)
            .WithHandler(new DelegateHandler(message => handled++))
            .Build();

        var first = await inbox.ProcessAsync(new SampleMessage("a", 1));
        var second = await inbox.ProcessAsync(new SampleMessage("a", 2));

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(1, handled);
    }

    [Fact]
    public async Task RemovesKeyWhenHandlerFails()
    {
        var attempts = 0;
        var inbox = new InboxBuilder<SampleMessage, string>()
            .WithKeySelector(message => message.Id)
            .WithStore(new InMemoryInboxStore<string>())
            .WithHandler(new DelegateHandler(_ =>
            {
                if (attempts++ == 0)
                    throw new InvalidOperationException("boom");
            }))
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await inbox.ProcessAsync(new SampleMessage("a", 1)));

        var retry = await inbox.ProcessAsync(new SampleMessage("a", 2));
        Assert.True(retry);
    }

    [Fact]
    public async Task CleansUpExpiredKeys()
    {
        var store = new InMemoryInboxStore<string>();
        var registered = await store.TryRegisterAsync("a", DateTimeOffset.UtcNow.AddSeconds(-1));
        var removed = await store.CleanupExpiredAsync(DateTimeOffset.UtcNow);

        Assert.True(registered);
        Assert.Equal(1, removed);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddInboxFramework();

        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IInboxStore<string>>();

        Assert.NotNull(store);
    }

    private sealed class DelegateHandler : IInboxHandler<SampleMessage>
    {
        private readonly Action<SampleMessage> _action;

        public DelegateHandler(Action<SampleMessage> action)
        {
            _action = action;
        }

        public ValueTask HandleAsync(SampleMessage message, CancellationToken cancellationToken = default)
        {
            _action(message);
            return ValueTask.CompletedTask;
        }
    }

    private sealed record SampleMessage(string Id, int Version);
}
