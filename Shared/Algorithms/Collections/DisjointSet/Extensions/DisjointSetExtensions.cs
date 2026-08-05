using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Algorithms.Collections.DisjointSet.Abstractions;
using Usm.Shared.Algorithms.Collections.DisjointSet.Builders;

namespace Usm.Shared.Algorithms.Collections.DisjointSet.Extensions;

/// <summary>
/// Common extension methods for disjoint set registration.
/// </summary>
public static class DisjointSetExtensions
{
    /// <summary>Registers the disjoint set builder.</summary>
    public static IServiceCollection AddDisjointSetFramework(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(DisjointSetBuilder<>), typeof(DisjointSetBuilder<>));
        return services;
    }
}
