using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.Plugin.Abstractions;
using Usm.Shared.Patterns.Plugin.Builders;
using Usm.Shared.Patterns.Plugin.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.Plugin.Tests;

public sealed class PluginTests
{
    [Fact]
    public void DiscoversAndOrdersPluginsByDependencies()
    {
        var discovery = new ReflectionPluginDiscovery();
        var registry = new InMemoryPluginRegistry(new PluginOptions());
        foreach (var descriptor in discovery.Discover(typeof(BetaPlugin).Assembly))
            registry.Register(descriptor);

        var ordered = registry.GetOrderedPlugins();

        Assert.Equal(new[] { "Alpha", "Beta" }, ordered.Select(p => p.Name));
    }

    [Fact]
    public async Task InitializesAndShutsDownPlugins()
    {
        PluginLog.Events.Clear();

        var host = new PluginBuilder()
            .AddPlugin<AlphaPlugin>()
            .AddPlugin<BetaPlugin>()
            .Build();

        await host.InitializeAsync();
        await host.ShutdownAsync();

        Assert.Equal(new[] { "alpha:init", "beta:init", "beta:shutdown", "alpha:shutdown" }, PluginLog.Events.ToArray());
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddPluginFramework();

        using var provider = services.BuildServiceProvider();
        var discovery = provider.GetRequiredService<IPluginDiscovery>();

        Assert.NotNull(discovery);
    }

    public sealed class AlphaPlugin : IPlugin
    {
        public string Name => "Alpha";
        public Version Version => new(1, 0);
        public IReadOnlyCollection<string> Dependencies => Array.Empty<string>();

        public ValueTask InitializeAsync(PluginContext context, CancellationToken cancellationToken = default)
        {
            PluginLog.Events.Add("alpha:init");
            return ValueTask.CompletedTask;
        }

        public ValueTask ShutdownAsync(PluginContext context, CancellationToken cancellationToken = default)
        {
            PluginLog.Events.Add("alpha:shutdown");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class BetaPlugin : IPlugin
    {
        public string Name => "Beta";
        public Version Version => new(1, 0);
        public IReadOnlyCollection<string> Dependencies => new[] { "Alpha" };

        public ValueTask InitializeAsync(PluginContext context, CancellationToken cancellationToken = default)
        {
            PluginLog.Events.Add("beta:init");
            return ValueTask.CompletedTask;
        }

        public ValueTask ShutdownAsync(PluginContext context, CancellationToken cancellationToken = default)
        {
            PluginLog.Events.Add("beta:shutdown");
            return ValueTask.CompletedTask;
        }
    }

    private static class PluginLog
    {
        public static readonly List<string> Events = new();
    }
}
