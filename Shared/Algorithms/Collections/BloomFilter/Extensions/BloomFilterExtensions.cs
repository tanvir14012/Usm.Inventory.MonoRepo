using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Algorithms.Collections.BloomFilter.Abstractions;
using Usm.Shared.Algorithms.Collections.BloomFilter.Builders;

namespace Usm.Shared.Algorithms.Collections.BloomFilter.Extensions;

/// <summary>
/// Common extension methods for Bloom filter registration.
/// </summary>
public static class BloomFilterExtensions
{
    /// <summary>Registers the Bloom filter builder.</summary>
    public static IServiceCollection AddBloomFilterFramework(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(BloomFilterBuilder<>), typeof(BloomFilterBuilder<>));
        return services;
    }
}
