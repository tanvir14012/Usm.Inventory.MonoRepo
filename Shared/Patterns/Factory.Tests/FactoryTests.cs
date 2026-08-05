using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.Factory.Abstractions;
using Usm.Shared.Patterns.Factory.Builders;
using Usm.Shared.Patterns.Factory.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.Factory.Tests;

public sealed class FactoryTests
{
    [Fact]
    public void CreatesProductsFromExpressionAndDelegate()
    {
        var factory = Factory<OrderContext, OrderDto>.From(ctx => new OrderDto(ctx.Id, ctx.Total));
        var dto = factory.Create(new OrderContext(7, 42m));

        Assert.Equal(7, dto.Id);
        Assert.Equal(42m, dto.Total);
    }

    [Fact]
    public async Task CreatesProductsAsynchronously()
    {
        var factory = Factory<OrderContext, OrderDto>.FromAsync(static async (ctx, token) =>
        {
            await Task.Delay(1, token);
            return new OrderDto(ctx.Id, ctx.Total);
        });

        var dto = await factory.CreateAsync(new OrderContext(9, 12m));

        Assert.Equal(9, dto.Id);
        Assert.Equal(12m, dto.Total);
        Assert.False(factory.CanCreateSynchronously);
    }

    [Fact]
    public void BuildsFactoryFromBuilder()
    {
        var builder = new FactoryBuilder<OrderContext, OrderDto>();
        var factory = builder.UseExpression(ctx => new OrderDto(ctx.Id, ctx.Total)).Build();

        Assert.Equal(new OrderDto(5, 100m), factory.Create(new OrderContext(5, 100m)));
    }

    [Fact]
    public void ConvertsToExpressionAndCompiles()
    {
        var factory = Factory<OrderContext, OrderDto>.From(ctx => new OrderDto(ctx.Id, ctx.Total));

        var expression = factory.ToExpression();
        var compiled = factory.Compile();

        Assert.Equal(new OrderDto(1, 2m), expression.Compile()(new OrderContext(1, 2m)));
        Assert.Equal(new OrderDto(1, 2m), compiled(new OrderContext(1, 2m)));
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddFactoryFramework();

        using var provider = services.BuildServiceProvider();
        var compiler = provider.GetRequiredService<IFactoryCompiler<OrderContext, OrderDto>>();
        var factory = Factory<OrderContext, OrderDto>.From(ctx => new OrderDto(ctx.Id, ctx.Total));
        var compiled = compiler.Compile(factory);

        Assert.Equal(new OrderDto(3, 9m), compiled(new OrderContext(3, 9m)));
    }

    private sealed record OrderContext(int Id, decimal Total);

    private sealed record OrderDto(int Id, decimal Total);
}
