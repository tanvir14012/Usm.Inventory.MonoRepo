using Usm.Shared.Patterns.Plugin.Abstractions;

namespace Usm.Shared.Patterns.Plugin;

/// <summary>
/// Describes a discovered plugin.
/// </summary>
public sealed record PluginDescriptor(
    Type PluginType,
    string Name,
    Version Version,
    IReadOnlyCollection<string> Dependencies)
{
    /// <summary>Creates a plugin instance.</summary>
    public IPlugin CreateInstance()
        => Activator.CreateInstance(PluginType) is IPlugin plugin
            ? plugin
            : throw new InvalidOperationException($"Unable to create plugin instance for {PluginType.FullName}.");
}

/// <summary>
/// Plugin host configuration.
/// </summary>
public sealed class PluginOptions
{
    /// <summary>Gets or sets a value indicating whether version conflicts should be rejected.</summary>
    public bool EnforceUniqueNames { get; set; } = true;
}
