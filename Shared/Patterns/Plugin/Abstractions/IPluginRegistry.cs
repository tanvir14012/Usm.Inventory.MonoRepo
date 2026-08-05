namespace Usm.Shared.Patterns.Plugin.Abstractions;

/// <summary>
/// Stores discovered plugins and resolves dependency order.
/// </summary>
public interface IPluginRegistry
{
    /// <summary>Registers a plugin descriptor.</summary>
    void Register(PluginDescriptor descriptor);

    /// <summary>Returns all registered plugins in dependency order.</summary>
    IReadOnlyList<PluginDescriptor> GetOrderedPlugins();
}
