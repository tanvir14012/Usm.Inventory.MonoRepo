using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Algorithms.Collections.SegmentTree.Abstractions;
using Usm.Shared.Algorithms.Collections.SegmentTree.Builders;

namespace Usm.Shared.Algorithms.Collections.SegmentTree.Extensions;

/// <summary>
/// Common extension methods for segment tree creation.
/// </summary>
public static class SegmentTreeExtensions
{
    /// <summary>Creates a new builder.</summary>
    public static ISegmentTreeBuilder<T> CreateBuilder<T>()
        where T : INumber<T>
        => new SegmentTreeBuilder<T>();

    /// <summary>Registers the builder.</summary>
    public static IServiceCollection AddSegmentTreeFramework(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(SegmentTreeBuilder<>), typeof(SegmentTreeBuilder<>));
        return services;
    }
}
