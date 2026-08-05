using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.Retry;
using Usm.Shared.Patterns.Retry.Abstractions;
using Usm.Shared.Patterns.Retry.Builders;
using Usm.Shared.Patterns.Retry.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.Retry.Tests;

public sealed class RetryTests
{
    [Fact]
    public async Task RetriesUntilSuccess()
    {
        var attempts = 0;
        var policy = new RetryBuilder<string, string>()
            .WithMaxAttempts(3)
            .WithDelay(TimeSpan.Zero)
            .Build();

        var result = await policy.ExecuteAsync("x", async (_, token) =>
        {
            await Task.CompletedTask;
            attempts++;
            return attempts < 2 ? throw new InvalidOperationException("transient") : "ok";
        });

        Assert.Equal("ok", result);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public void ExecutesSynchronously()
    {
        var policy = new RetryBuilder<int, int>()
            .WithMaxAttempts(2)
            .Build();

        var result = policy.Execute(4, value => value * 2);

        Assert.Equal(8, result);
    }

    [Fact]
    public async Task SupportsCustomDelayStrategy()
    {
        var policy = new RetryBuilder<int, int>()
            .WithCustomDelayStrategy(attempt => TimeSpan.FromMilliseconds(attempt))
            .Build();

        var result = await policy.ExecuteAsync(1, static async (_, token) =>
        {
            await Task.Delay(1, token);
            return 1;
        });

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task UsesJitterAndExponentialStrategy()
    {
        var policy = new RetryBuilder<int, int>()
            .WithMaxAttempts(2)
            .WithDelay(TimeSpan.FromMilliseconds(1))
            .WithStrategy(RetryStrategy.Exponential)
            .WithJitter(true)
            .Build();

        var attempts = 0;
        var result = await policy.ExecuteAsync(1, async (_, token) =>
        {
            await Task.Delay(1, token);
            attempts++;
            return attempts < 2 ? throw new InvalidOperationException("transient") : 1;
        });

        Assert.Equal(1, result);
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddRetryFramework();

        using var provider = services.BuildServiceProvider();
        var builder = provider.GetRequiredService<RetryBuilder<string, string>>();

        Assert.NotNull(builder);
    }
}
