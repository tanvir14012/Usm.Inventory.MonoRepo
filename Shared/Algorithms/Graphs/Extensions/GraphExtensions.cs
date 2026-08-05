using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Algorithms.Graphs.Abstractions;
using Usm.Shared.Algorithms.Graphs.Builders;

namespace Usm.Shared.Algorithms.Graphs.Extensions;

/// <summary>
/// Common extension methods for graph creation.
/// </summary>
public static class GraphExtensions
{
    /// <summary>Creates a new builder.</summary>
    public static IGraphBuilder<TVertex, TWeight> CreateBuilder<TVertex, TWeight>()
        where TVertex : notnull
        where TWeight : INumber<TWeight>
        => new GraphBuilder<TVertex, TWeight>();

    /// <summary>Registers the builder.</summary>
    public static IServiceCollection AddGraphFramework(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(GraphBuilder<,>), typeof(GraphBuilder<,>));
        return services;
    }
}
