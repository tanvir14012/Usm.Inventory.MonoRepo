using Microsoft.Extensions.DependencyInjection;

namespace Usm.Shared.BuildingBlocks.Localization;

public static class LocalizationServiceCollectionExtensions
{
    public static IServiceCollection AddResxLocalization(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        return services;
    }
}
