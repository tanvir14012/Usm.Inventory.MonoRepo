namespace Usm.Shared.Patterns.Plugin.Abstractions;

/// <summary>
/// Hosts plugin lifecycle and ordering.
/// </summary>
public interface IPluginHost
{
    /// <summary>Gets the registered plugin descriptors in execution order.</summary>
    IReadOnlyList<PluginDescriptor> Plugins { get; }

    /// <summary>Initializes all plugins in dependency order.</summary>
    ValueTask InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Shuts down all plugins in reverse dependency order.</summary>
    ValueTask ShutdownAsync(CancellationToken cancellationToken = default);
}
