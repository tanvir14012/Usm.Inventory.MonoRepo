using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.StateMachine.Abstractions;
using Usm.Shared.Patterns.StateMachine.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.StateMachine.Tests;

public sealed class StateMachineTests
{
    [Fact]
    public void FiresConfiguredTransition()
    {
        var machine = StateMachine<OrderState, OrderTrigger>.CreateBuilder()
            .Configure(OrderState.Draft, state => state
                .Permit(OrderTrigger.Submit, OrderState.Submitted))
            .Build(OrderState.Draft);

        Assert.True(machine.CanFire(OrderTrigger.Submit));
        Assert.Equal(OrderState.Submitted, machine.Fire(OrderTrigger.Submit));
        Assert.Equal(OrderState.Submitted, machine.CurrentState);
    }

    [Fact]
    public async Task FiresAsyncTransition()
    {
        var machine = StateMachine<OrderState, OrderTrigger>.CreateBuilder()
            .Configure(OrderState.Draft, state => state
                .Permit(OrderTrigger.Submit, OrderState.Submitted)
                .OnExit(static async (_, token) =>
                {
                    await Task.Delay(1, token);
                }))
            .Build(OrderState.Draft);

        var result = await machine.FireAsync(OrderTrigger.Submit);

        Assert.Equal(OrderState.Submitted, result);
    }

    [Fact]
    public void IgnoresConfiguredTrigger()
    {
        var machine = StateMachine<OrderState, OrderTrigger>.CreateBuilder()
            .Configure(OrderState.Draft, state => state
                .Ignore(OrderTrigger.Submit))
            .Build(OrderState.Draft);

        Assert.True(machine.CanFire(OrderTrigger.Submit));
        Assert.Equal(OrderState.Draft, machine.Fire(OrderTrigger.Submit));
    }

    [Fact]
    public void RunsEntryAndExitActions()
    {
        var trace = new List<string>();
        var machine = StateMachine<OrderState, OrderTrigger>.CreateBuilder()
            .Configure(OrderState.Draft, state => state
                .Permit(OrderTrigger.Submit, OrderState.Submitted)
                .OnExit(s => trace.Add($"exit:{s}")))
            .Configure(OrderState.Submitted, state => state
                .OnEntry(s => trace.Add($"enter:{s}")))
            .Build(OrderState.Draft);

        machine.Fire(OrderTrigger.Submit);

        Assert.Equal(new[] { "exit:Draft", "enter:Submitted" }, trace);
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddStateMachineFramework();

        using var provider = services.BuildServiceProvider();
        var builder = provider.GetRequiredService<IStateMachineBuilder<OrderState, OrderTrigger>>();
        var machine = builder
            .Configure(OrderState.Draft, state => state.Permit(OrderTrigger.Submit, OrderState.Submitted))
            .Build(OrderState.Draft);

        Assert.Equal(OrderState.Submitted, machine.Fire(OrderTrigger.Submit));
    }

    private enum OrderState
    {
        Draft,
        Submitted
    }

    private enum OrderTrigger
    {
        Submit
    }
}
