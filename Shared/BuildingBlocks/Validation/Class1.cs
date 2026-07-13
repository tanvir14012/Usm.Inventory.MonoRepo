using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Usm.Shared.BuildingBlocks.Validation;

public static class ValidationExtensions
{
    public static IServiceCollection AddAssemblyValidators<TMarker>(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<TMarker>(ServiceLifetime.Scoped, includeInternalTypes: false);
        return services;
    }
}
