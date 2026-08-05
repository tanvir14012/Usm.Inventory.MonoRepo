namespace Usm.Shared.Patterns.Plugin.Abstractions;

/// <summary>
/// Fluent builder for plugin host configuration.
/// </summary>
public interface IPluginBuilder
{
    /// <summary>Adds an assembly to scan for plugins.</summary>
    IPluginBuilder ScanAssembly(System.Reflection.Assembly assembly);

    /// <summary>Adds a plugin type explicitly.</summary>
    IPluginBuilder AddPlugin<TPlugin>() where TPlugin : class, IPlugin, new();

    /// <summary>Sets the plugin context accessor.</summary>
    IPluginBuilder WithContext(IServiceProvider serviceProvider);

    /// <summary>Sets the logger.</summary>
    IPluginBuilder WithLogger(Microsoft.Extensions.Logging.ILogger<IPluginHost> logger);

    /// <summary>Builds the host.</summary>
    IPluginHost Build();
}
