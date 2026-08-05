using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.RuleEngine.Abstractions;
using Usm.Shared.Patterns.RuleEngine.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.RuleEngine.Tests;

public sealed class RuleEngineTests
{
    [Fact]
    public void EvaluatesHighestPriorityRule()
    {
        var engine = RuleEngine<OrderContext, string>.CreateBuilder()
            .WhenPredicate(ctx => ctx.Amount >= 100, _ => "High", priority: 10, group: "risk")
            .WhenPredicate(ctx => ctx.Amount >= 50, _ => "Medium", priority: 5, group: "risk")
            .OtherwisePredicate(_ => "Low", group: "risk")
            .Build();

        Assert.Equal("High", engine.Evaluate(new OrderContext(120m), group: "risk"));
        Assert.Equal("Low", engine.Evaluate(new OrderContext(20m), group: "risk"));
    }

    [Fact]
    public async Task EvaluatesAsyncRules()
    {
        var engine = RuleEngine<OrderContext, string>.CreateBuilder()
            .WhenAsync(
                static async (ctx, token) =>
                {
                    await Task.Delay(1, token);
                    return ctx.Amount >= 100;
                },
                static async (ctx, token) =>
                {
                    await Task.Delay(1, token);
                    return "High";
                },
                priority: 10)
            .OtherwiseAsync(static async (_, token) =>
            {
                await Task.Delay(1, token);
                return "Low";
            })
            .Build();

        Assert.Equal("High", await engine.EvaluateAsync(new OrderContext(120m)));
        Assert.False(engine.CanExecuteSynchronously);
    }

    [Fact]
    public void ConvertsToExpressionAndCompiles()
    {
        var engine = RuleEngine<OrderContext, string>.CreateBuilder()
            .WhenExpression(ctx => ctx.Amount >= 100, ctx => "High", priority: 10)
            .OtherwiseExpression(_ => "Low")
            .Build();

        var expression = engine.ToExpression();
        var compiled = engine.Compile();

        Assert.Equal("High", expression.Compile()(new OrderContext(150m)));
        Assert.Equal("High", compiled(new OrderContext(150m)));
    }

    [Fact]
    public void ThrowsWhenNoRuleMatchesWithoutFallback()
    {
        var engine = RuleEngine<OrderContext, string>.CreateBuilder()
            .WhenPredicate(ctx => ctx.Amount >= 100, _ => "High")
            .Build();

        Assert.Throws<InvalidOperationException>(() => engine.Evaluate(new OrderContext(20m)));
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddRuleEngineFramework();

        using var provider = services.BuildServiceProvider();
        var compiler = provider.GetRequiredService<IRuleCompiler<OrderContext, string>>();
        var engine = RuleEngine<OrderContext, string>.CreateBuilder()
            .WhenPredicate(ctx => ctx.Amount >= 100, _ => "High")
            .OtherwisePredicate(_ => "Low")
            .Build();

        var compiled = compiler.Compile(engine);

        Assert.Equal("High", compiled(new OrderContext(150m)));
    }

    private sealed record OrderContext(decimal Amount);
}
