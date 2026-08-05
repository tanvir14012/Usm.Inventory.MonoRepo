using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Algorithms.Collections.IntervalTree.Abstractions;
using Usm.Shared.Algorithms.Collections.IntervalTree.Builders;

namespace Usm.Shared.Algorithms.Collections.IntervalTree.Extensions;

/// <summary>
/// Common extension methods for interval tree creation.
/// </summary>
public static class IntervalTreeExtensions
{
    /// <summary>Creates a new builder.</summary>
    public static IIntervalTreeBuilder<TKey, TValue> CreateBuilder<TKey, TValue>()
        where TKey : notnull
        => new IntervalTreeBuilder<TKey, TValue>();

    /// <summary>Registers the builder.</summary>
    public static IServiceCollection AddIntervalTreeFramework(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(IntervalTreeBuilder<,>), typeof(IntervalTreeBuilder<,>));
        return services;
    }
}
