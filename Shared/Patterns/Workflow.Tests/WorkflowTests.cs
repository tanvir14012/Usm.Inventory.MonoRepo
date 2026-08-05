using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.Workflow.Abstractions;
using Usm.Shared.Patterns.Workflow.Configuration;
using Usm.Shared.Patterns.Workflow.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.Workflow.Tests;

public sealed class WorkflowTests
{
    [Fact]
    public void ExecutesSequentialWorkflow()
    {
        var workflow = Workflow<OrderContext>.CreateBuilder()
            .Then(ctx => ctx with { Total = ctx.Subtotal + ctx.Tax })
            .Then(ctx => ctx with { Approved = true })
            .Build();

        var result = workflow.Execute(new OrderContext(100m, 15m, false));

        Assert.Equal(115m, result.Total);
        Assert.True(result.Approved);
    }

    [Fact]
    public async Task ExecutesConditionalAndAsyncWorkflow()
    {
        var workflow = Workflow<OrderContext>.CreateBuilder()
            .When(ctx => ctx.RequiresApproval, thenBranch =>
                thenBranch.Then(ctx => ctx with { Approved = true }))
            .ThenAsync(static async (ctx, token) =>
            {
                await Task.Delay(1, token);
                return ctx with { Total = ctx.Subtotal + ctx.Tax };
            })
            .Build();

        var result = await workflow.ExecuteAsync(new OrderContext(100m, 15m, true));

        Assert.True(result.Approved);
        Assert.Equal(115m, result.Total);
        Assert.False(workflow.CanExecuteSynchronously);
    }

    [Fact]
    public async Task ExecutesParallelBranches()
    {
        var workflow = Workflow<OrderContext>.CreateBuilder()
            .Parallel(
                [
                    static async (ctx, token) =>
                    {
                        await Task.Delay(1, token);
                        return ctx with { Total = ctx.Total + 10m };
                    },
                    static async (ctx, token) =>
                    {
                        await Task.Delay(1, token);
                        return ctx with { Total = ctx.Total + 20m };
                    }
                ],
                (_, results) => results[^1])
            .Build();

        var result = await workflow.ExecuteAsync(new OrderContext(100m, 15m, false));

        Assert.Equal(20m, result.Total);
    }

    [Fact]
    public async Task RetriesTransientFailure()
    {
        var attempts = 0;
        var workflow = Workflow<OrderContext>.CreateBuilder()
            .Retry(async (ctx, token) =>
            {
                attempts++;
                await Task.Delay(1, token);
                return attempts < 2 ? throw new InvalidOperationException("Transient") : ctx with { Approved = true };
            }, new RetryOptions { MaxAttempts = 3 })
            .Build();

        var result = await workflow.ExecuteAsync(new OrderContext(100m, 15m, false));

        Assert.True(result.Approved);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ExecutesCompensationOnFailure()
    {
        var log = new List<string>();
        var workflow = Workflow<OrderContext>.CreateBuilder()
            .Compensate(
                async (ctx, token) =>
                {
                    await Task.Delay(1, token);
                    log.Add("step");
                    return ctx with { Approved = true };
                },
                (ctx, token) =>
                {
                    log.Add($"compensate:{ctx.Approved}");
                    return ValueTask.CompletedTask;
                })
            .Then(ctx => throw new InvalidOperationException("boom"))
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.ExecuteAsync(new OrderContext(100m, 15m, false)).AsTask());
        Assert.Contains("compensate:True", log);
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddWorkflowFramework();

        using var provider = services.BuildServiceProvider();
        var compiler = provider.GetRequiredService<IWorkflowCompiler<OrderContext>>();
        var workflow = Workflow<OrderContext>.CreateBuilder()
            .Then(ctx => ctx with { Approved = true })
            .Build();

        var compiled = compiler.Compile(workflow);

        Assert.True(compiled(new OrderContext(1m, 0m, false)).Approved);
    }

    private sealed record OrderContext(decimal Subtotal, decimal Tax, bool RequiresApproval)
    {
        public decimal Total { get; init; }
        public bool Approved { get; init; }
    }
}
