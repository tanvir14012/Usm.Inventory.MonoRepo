using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.Saga.Abstractions;
using Usm.Shared.Patterns.Saga.Builders;
using Usm.Shared.Patterns.Saga.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.Saga.Tests;

public sealed class SagaTests
{
    [Fact]
    public async Task ExecutesSuccessfulSaga()
    {
        var saga = new SagaBuilder<OrderContext>()
            .WithSagaId("order")
            .Use((ctx, ct) => ValueTask.FromResult(ctx with { Reserved = true }))
            .Use((ctx, ct) => ValueTask.FromResult(ctx with { Paid = true }))
            .Build();

        var result = await saga.ExecuteAsync(new OrderContext());

        Assert.True(result.Succeeded);
        Assert.True(result.Context.Reserved);
        Assert.True(result.Context.Paid);
    }

    [Fact]
    public async Task CompensatesOnFailure()
    {
        var events = new List<string>();
        var saga = new SagaBuilder<OrderContext>()
            .WithSagaId("order")
            .Use((ctx, ct) => ValueTask.FromResult(ctx with { Reserved = true }), (ctx, ct) =>
            {
                events.Add("release");
                return ValueTask.CompletedTask;
            })
            .Use((ctx, ct) => throw new InvalidOperationException("boom"))
            .Build();

        var result = await saga.ExecuteAsync(new OrderContext());

        Assert.False(result.Succeeded);
        Assert.Contains("release", events);
    }

    [Fact]
    public void PersistsSnapshotsViaDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddSagaFramework();

        using var provider = services.BuildServiceProvider();
        var persistence = provider.GetRequiredService<ISagaPersistence<OrderContext>>();

        Assert.NotNull(persistence);
    }

    private sealed record OrderContext
    {
        public bool Reserved { get; init; }

        public bool Paid { get; init; }
    }
}
