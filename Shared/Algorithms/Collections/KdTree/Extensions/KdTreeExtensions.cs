using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Algorithms.Collections.KdTree.Abstractions;
using Usm.Shared.Algorithms.Collections.KdTree.Builders;

namespace Usm.Shared.Algorithms.Collections.KdTree.Extensions;

/// <summary>
/// Common extension methods for KD-tree creation.
/// </summary>
public static class KdTreeExtensions
{
    /// <summary>Creates a new builder.</summary>
    public static IKdTreeBuilder<TCoordinate, TValue> CreateBuilder<TCoordinate, TValue>()
        where TCoordinate : IFloatingPointIeee754<TCoordinate>
        => new KdTreeBuilder<TCoordinate, TValue>();

    /// <summary>Registers the builder.</summary>
    public static IServiceCollection AddKdTreeFramework(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(KdTreeBuilder<,>), typeof(KdTreeBuilder<,>));
        return services;
    }
}
