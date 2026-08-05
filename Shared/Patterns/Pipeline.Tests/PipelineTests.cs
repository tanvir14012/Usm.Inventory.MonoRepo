using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.Pipeline.Abstractions;
using Usm.Shared.Patterns.Pipeline.Builders;
using Usm.Shared.Patterns.Pipeline.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.Pipeline.Tests;

public sealed class PipelineTests
{
    [Fact]
    public void ExecutesSyncPipeline()
    {
        var pipeline = Pipeline<InvoiceContext>.CreateBuilder()
            .Use(ctx => new InvoiceContext(ctx.Id, ctx.Amount + ctx.Tax, ctx.Tax))
            .Then(ctx => new InvoiceContext(ctx.Id, decimal.Round(ctx.Amount, 2), ctx.Tax))
            .Build();

        var result = pipeline.Execute(new InvoiceContext(1, 100m, 15m));

        Assert.Equal(115m, result.Amount);
    }

    [Fact]
    public async Task ExecutesAsyncPipeline()
    {
        var pipeline = Pipeline<InvoiceContext>.CreateBuilder()
            .UseAsync(static async (ctx, token) =>
            {
                await Task.Delay(1, token);
                return new InvoiceContext(ctx.Id, ctx.Amount + ctx.Tax, ctx.Tax);
            })
            .ThenAsync(static async (ctx, token) =>
            {
                await Task.Delay(1, token);
                return new InvoiceContext(ctx.Id, decimal.Round(ctx.Amount, 2), ctx.Tax);
            })
            .Build();

        var result = await pipeline.ExecuteAsync(new InvoiceContext(1, 100m, 15m));

        Assert.Equal(115m, result.Amount);
        Assert.False(pipeline.CanExecuteSynchronously);
    }

    [Fact]
    public void RunsFinalizer()
    {
        var seen = 0;
        var pipeline = Pipeline<InvoiceContext>.CreateBuilder()
            .Use(ctx => ctx)
            .Finally(ctx => seen = ctx.Id)
            .Build();

        var result = pipeline.Execute(new InvoiceContext(7, 10m, 0m));

        Assert.Equal(7, seen);
        Assert.Equal(7, result.Id);
    }

    [Fact]
    public void ConvertsToExpressionAndCompiles()
    {
        var pipeline = Pipeline<InvoiceContext>.CreateBuilder()
            .Use(ctx => new InvoiceContext(ctx.Id, ctx.Amount + ctx.Tax, ctx.Tax))
            .Then(ctx => new InvoiceContext(ctx.Id, decimal.Round(ctx.Amount, 2), ctx.Tax))
            .Build();

        var expression = pipeline.ToExpression();
        var compiled = pipeline.Compile();

        Assert.Equal(115m, expression.Compile()(new InvoiceContext(1, 100m, 15m)).Amount);
        Assert.Equal(115m, compiled(new InvoiceContext(1, 100m, 15m)).Amount);
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddPipelineFramework();

        using var provider = services.BuildServiceProvider();
        var compiler = provider.GetRequiredService<IPipelineCompiler<InvoiceContext>>();
        var pipeline = Pipeline<InvoiceContext>.CreateBuilder()
            .Use(ctx => new InvoiceContext(ctx.Id, ctx.Amount + ctx.Tax, ctx.Tax))
            .Build();

        var compiled = compiler.Compile(pipeline);

        Assert.Equal(115m, compiled(new InvoiceContext(1, 100m, 15m)).Amount);
    }

    private sealed record InvoiceContext(int Id, decimal Amount, decimal Tax);
}
