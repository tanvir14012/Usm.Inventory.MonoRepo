using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Algorithms.Distributed.Abstractions;
using Usm.Shared.Algorithms.Distributed.Builders;

namespace Usm.Shared.Algorithms.Distributed.Extensions;

/// <summary>
/// Common extension methods for distributed algorithm creation.
/// </summary>
public static class DistributedAlgorithmsExtensions
{
    /// <summary>Creates a new builder.</summary>
    public static IDistributedAlgorithmsBuilder CreateBuilder() => new DistributedAlgorithmsBuilder();

    /// <summary>Registers the builder.</summary>
    public static IServiceCollection AddDistributedAlgorithmsFramework(this IServiceCollection services)
    {
        services.TryAddTransient<DistributedAlgorithmsBuilder>();
        return services;
    }
}
