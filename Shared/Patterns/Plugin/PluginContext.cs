namespace Usm.Shared.Patterns.Plugin;

/// <summary>
/// Runtime plugin context.
/// </summary>
public sealed class PluginContext
{
    /// <summary>Initializes a new plugin context.</summary>
    public PluginContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>Gets the service provider.</summary>
    public IServiceProvider ServiceProvider { get; }
}
