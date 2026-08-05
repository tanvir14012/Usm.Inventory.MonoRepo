using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Algorithms.Compression.Abstractions;
using Usm.Shared.Algorithms.Compression.Builders;

namespace Usm.Shared.Algorithms.Compression.Extensions;

/// <summary>
/// Common extension methods for compression algorithm creation.
/// </summary>
public static class CompressionAlgorithmsExtensions
{
    /// <summary>Creates a new builder.</summary>
    public static ICompressionAlgorithmsBuilder CreateBuilder() => new CompressionAlgorithmsBuilder();

    /// <summary>Registers the builder.</summary>
    public static IServiceCollection AddCompressionAlgorithmsFramework(this IServiceCollection services)
    {
        services.TryAddTransient<CompressionAlgorithmsBuilder>();
        return services;
    }
}
