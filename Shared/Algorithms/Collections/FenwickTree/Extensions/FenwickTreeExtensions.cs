using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Algorithms.Collections.FenwickTree.Abstractions;
using Usm.Shared.Algorithms.Collections.FenwickTree.Builders;

namespace Usm.Shared.Algorithms.Collections.FenwickTree.Extensions;

/// <summary>
/// Common extension methods for Fenwick tree creation.
/// </summary>
public static class FenwickTreeExtensions
{
    /// <summary>Creates a new builder.</summary>
    public static IFenwickTreeBuilder<T> CreateBuilder<T>()
        where T : INumber<T>
        => new FenwickTreeBuilder<T>();

    /// <summary>Registers the Fenwick tree builder.</summary>
    public static IServiceCollection AddFenwickTreeFramework(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(FenwickTreeBuilder<>), typeof(FenwickTreeBuilder<>));
        return services;
    }
}
