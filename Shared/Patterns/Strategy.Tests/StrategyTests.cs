using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.Strategy.Abstractions;
using Usm.Shared.Patterns.Strategy.Builders;
using Usm.Shared.Patterns.Strategy.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.Strategy.Tests;

public sealed class StrategyTests
{
    [Fact]
    public void ExecutesStrategyFromExpressionAndDelegate()
    {
        var strategy = Strategy<PriceContext, decimal>.From(ctx => ctx.BasePrice * (1 - ctx.Discount));
        var total = strategy.Execute(new PriceContext(100m, 0.15m));

        Assert.Equal(85m, total);
    }

    [Fact]
    public async Task ExecutesStrategyAsynchronously()
    {
        var strategy = Strategy<PriceContext, decimal>.FromAsync(static async (ctx, token) =>
        {
            await Task.Delay(1, token);
            return ctx.BasePrice * (1 - ctx.Discount);
        });

        var total = await strategy.ExecuteAsync(new PriceContext(200m, 0.1m));

        Assert.Equal(180m, total);
        Assert.False(strategy.CanExecuteSynchronously);
    }

    [Fact]
    public void BuildsStrategyFromBuilder()
    {
        var builder = new StrategyBuilder<PriceContext, decimal>();
        var strategy = builder.UseExpression(ctx => ctx.BasePrice * (1 - ctx.Discount)).Build();

        Assert.Equal(60m, strategy.Execute(new PriceContext(75m, 0.2m)));
    }

    [Fact]
    public void ConvertsToExpressionAndCompiles()
    {
        var strategy = Strategy<PriceContext, decimal>.From(ctx => ctx.BasePrice * (1 - ctx.Discount));

        var expression = strategy.ToExpression();
        var compiled = strategy.Compile();

        Assert.Equal(40m, expression.Compile()(new PriceContext(50m, 0.2m)));
        Assert.Equal(40m, compiled(new PriceContext(50m, 0.2m)));
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddStrategyFramework();

        using var provider = services.BuildServiceProvider();
        var compiler = provider.GetRequiredService<IStrategyCompiler<PriceContext, decimal>>();
        var strategy = Strategy<PriceContext, decimal>.From(ctx => ctx.BasePrice * (1 - ctx.Discount));
        var compiled = compiler.Compile(strategy);

        Assert.Equal(72m, compiled(new PriceContext(90m, 0.2m)));
    }

    private sealed record PriceContext(decimal BasePrice, decimal Discount);
}
