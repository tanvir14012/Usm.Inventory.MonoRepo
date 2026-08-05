using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.Plugin;
using Usm.Shared.Patterns.Plugin.Abstractions;
using Usm.Shared.Patterns.Plugin.Builders;

namespace Usm.Shared.Patterns.Plugin.Extensions;

/// <summary>
/// Common extension methods for plugin host registration.
/// </summary>
public static class PluginExtensions
{
    /// <summary>Registers the plugin framework with dependency injection.</summary>
    public static IServiceCollection AddPluginFramework(this IServiceCollection services)
    {
        services.AddOptions<PluginOptions>();
        services.TryAddSingleton<IPluginDiscovery, ReflectionPluginDiscovery>();
        services.TryAddSingleton<IPluginRegistry>(sp => new InMemoryPluginRegistry(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PluginOptions>>().Value));
        services.TryAddTransient<IPluginBuilder, PluginBuilder>();
        return services;
    }
}

internal sealed class PluginHost : IPluginHost
{
    private readonly IPluginRegistry _registry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IPluginHost> _logger;
    private bool _initialized;

    public PluginHost(IPluginRegistry registry, IServiceProvider serviceProvider, ILogger<IPluginHost> logger)
    {
        _registry = registry;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IReadOnlyList<PluginDescriptor> Plugins => _registry.GetOrderedPlugins();

    public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return;

        var context = new PluginContext(_serviceProvider);
        foreach (var descriptor in Plugins)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var plugin = descriptor.CreateInstance();
            await plugin.InitializeAsync(context, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Initialized plugin {PluginName} {Version}.", descriptor.Name, descriptor.Version);
        }

        _initialized = true;
    }

    public async ValueTask ShutdownAsync(CancellationToken cancellationToken = default)
    {
        var context = new PluginContext(_serviceProvider);
        for (var i = Plugins.Count - 1; i >= 0; i--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var plugin = Plugins[i].CreateInstance();
            await plugin.ShutdownAsync(context, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Plugin host shut down.");
        _initialized = false;
    }
}

public sealed class ReflectionPluginDiscovery : IPluginDiscovery
{
    public IReadOnlyList<PluginDescriptor> Discover(Assembly assembly)
    {
        var results = new List<PluginDescriptor>();
        foreach (var type in assembly.DefinedTypes)
        {
            if (type.IsAbstract || type.IsInterface || !typeof(IPlugin).IsAssignableFrom(type))
                continue;

            if (type.GetConstructor(Type.EmptyTypes) is null)
                continue;

            var instance = (IPlugin?)Activator.CreateInstance(type.AsType());
            if (instance is null)
                continue;

            results.Add(new PluginDescriptor(type.AsType(), instance.Name, instance.Version, instance.Dependencies.ToArray()));
        }

        return results;
    }
}

public sealed class InMemoryPluginRegistry : IPluginRegistry
{
    private readonly Dictionary<string, PluginDescriptor> _plugins = new(StringComparer.OrdinalIgnoreCase);
    private readonly PluginOptions _options;

    public InMemoryPluginRegistry(PluginOptions options)
    {
        _options = options;
    }

    public void Register(PluginDescriptor descriptor)
    {
        if (_options.EnforceUniqueNames && _plugins.ContainsKey(descriptor.Name))
            throw new InvalidOperationException($"A plugin named '{descriptor.Name}' is already registered.");

        _plugins[descriptor.Name] = descriptor;
    }

    public IReadOnlyList<PluginDescriptor> GetOrderedPlugins()
    {
        var ordered = new List<PluginDescriptor>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var descriptor in _plugins.Values)
            Visit(descriptor, ordered, visited, visiting);

        return ordered;
    }

    private void Visit(PluginDescriptor descriptor, List<PluginDescriptor> ordered, HashSet<string> visited, HashSet<string> visiting)
    {
        if (visited.Contains(descriptor.Name))
            return;

        if (!visiting.Add(descriptor.Name))
            throw new InvalidOperationException($"Circular plugin dependency detected at '{descriptor.Name}'.");

        foreach (var dependencyName in descriptor.Dependencies)
        {
            if (!_plugins.TryGetValue(dependencyName, out var dependency))
                throw new InvalidOperationException($"Missing plugin dependency '{dependencyName}' for '{descriptor.Name}'.");

            Visit(dependency, ordered, visited, visiting);
        }

        visiting.Remove(descriptor.Name);
        visited.Add(descriptor.Name);
        ordered.Add(descriptor);
    }
}
