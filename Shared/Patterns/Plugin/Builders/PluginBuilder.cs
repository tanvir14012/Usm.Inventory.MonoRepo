using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usm.Shared.Patterns.Plugin.Abstractions;
using Usm.Shared.Patterns.Plugin.Extensions;

namespace Usm.Shared.Patterns.Plugin.Builders;

/// <summary>
/// Fluent builder for plugin host configuration.
/// </summary>
public sealed class PluginBuilder : IPluginBuilder
{
    private readonly List<Assembly> _assemblies = new();
    private readonly List<Type> _pluginTypes = new();
    private IServiceProvider? _serviceProvider;
    private ILogger<IPluginHost>? _logger;
    private readonly PluginOptions _options = new();

    /// <inheritdoc />
    public IPluginBuilder ScanAssembly(Assembly assembly)
    {
        _assemblies.Add(assembly ?? throw new ArgumentNullException(nameof(assembly)));
        return this;
    }

    /// <inheritdoc />
    public IPluginBuilder AddPlugin<TPlugin>() where TPlugin : class, IPlugin, new()
    {
        _pluginTypes.Add(typeof(TPlugin));
        return this;
    }

    /// <inheritdoc />
    public IPluginBuilder WithContext(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        return this;
    }

    /// <inheritdoc />
    public IPluginBuilder WithLogger(ILogger<IPluginHost> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        return this;
    }

    /// <inheritdoc />
    public IPluginHost Build()
    {
        var discovery = new ReflectionPluginDiscovery();
        var registry = new InMemoryPluginRegistry(_options);

        foreach (var assembly in _assemblies)
        {
            foreach (var descriptor in discovery.Discover(assembly))
                registry.Register(descriptor);
        }

        foreach (var pluginType in _pluginTypes)
        {
            var descriptor = new PluginDescriptor(pluginType, pluginType.Name, pluginType.Assembly.GetName().Version ?? new Version(1, 0), Array.Empty<string>());
            registry.Register(descriptor);
        }

        return new PluginHost(registry, _serviceProvider ?? new ServiceCollection().BuildServiceProvider(), _logger ?? NullLogger<IPluginHost>.Instance);
    }
}
