using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Algorithms.Collections.BTree.Abstractions;
using Usm.Shared.Algorithms.Collections.BTree.Builders;

namespace Usm.Shared.Algorithms.Collections.BTree.Extensions;

/// <summary>
/// Common extension methods for B-tree creation.
/// </summary>
public static class BTreeExtensions
{
    /// <summary>Creates a new builder.</summary>
    public static IBTreeBuilder<TKey, TValue> CreateBuilder<TKey, TValue>()
        where TKey : notnull
        => new BTreeBuilder<TKey, TValue>();

    /// <summary>Registers the builder.</summary>
    public static IServiceCollection AddBTreeFramework(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(BTreeBuilder<,>), typeof(BTreeBuilder<,>));
        return services;
    }
}
