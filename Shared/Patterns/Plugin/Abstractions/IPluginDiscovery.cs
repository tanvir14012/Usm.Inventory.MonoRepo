namespace Usm.Shared.Patterns.Plugin.Abstractions;

/// <summary>
/// Discovers plugins from assemblies.
/// </summary>
public interface IPluginDiscovery
{
    /// <summary>Discovers plugin descriptors from an assembly.</summary>
    IReadOnlyList<PluginDescriptor> Discover(System.Reflection.Assembly assembly);
}
