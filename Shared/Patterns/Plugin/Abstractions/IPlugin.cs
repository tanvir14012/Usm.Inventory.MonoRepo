using Usm.Shared.Patterns.Plugin;

namespace Usm.Shared.Patterns.Plugin.Abstractions;

/// <summary>
/// Represents a reusable plugin that can be initialized and shut down.
/// </summary>
public interface IPlugin
{
    /// <summary>Gets the plugin name.</summary>
    string Name { get; }

    /// <summary>Gets the plugin version.</summary>
    Version Version { get; }

    /// <summary>Gets the names of dependent plugins.</summary>
    IReadOnlyCollection<string> Dependencies { get; }

    /// <summary>Initializes the plugin.</summary>
    ValueTask InitializeAsync(PluginContext context, CancellationToken cancellationToken = default);

    /// <summary>Shuts the plugin down.</summary>
    ValueTask ShutdownAsync(PluginContext context, CancellationToken cancellationToken = default);
}
