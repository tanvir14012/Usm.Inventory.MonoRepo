using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usm.Shared.Algorithms.Parsing.Abstractions;
using Usm.Shared.Algorithms.Parsing.Builders;

namespace Usm.Shared.Algorithms.Parsing.Extensions;

/// <summary>
/// Common extension methods for parsing algorithm creation.
/// </summary>
public static class ParsingAlgorithmsExtensions
{
    /// <summary>Creates a new builder.</summary>
    public static IParsingAlgorithmsBuilder CreateBuilder() => new ParsingAlgorithmsBuilder();

    /// <summary>Registers the builder.</summary>
    public static IServiceCollection AddParsingAlgorithmsFramework(this IServiceCollection services)
    {
        services.TryAddTransient<ParsingAlgorithmsBuilder>();
        return services;
    }
}
