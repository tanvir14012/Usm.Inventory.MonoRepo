using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Algorithms.Strings.Abstractions;
using Usm.Shared.Algorithms.Strings.Builders;

namespace Usm.Shared.Algorithms.Strings.Extensions;

/// <summary>
/// Common extension methods for string algorithm creation.
/// </summary>
public static class StringAlgorithmsExtensions
{
    /// <summary>Creates a new builder.</summary>
    public static IStringAlgorithmsBuilder CreateBuilder() => new StringAlgorithmsBuilder();

    /// <summary>Registers the builder.</summary>
    public static IServiceCollection AddStringAlgorithmsFramework(this IServiceCollection services)
    {
        services.TryAddTransient<StringAlgorithmsBuilder>();
        return services;
    }
}
