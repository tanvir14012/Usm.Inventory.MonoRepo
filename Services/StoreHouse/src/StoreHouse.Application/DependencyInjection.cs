using MediatR;
using Microsoft.Extensions.DependencyInjection;
using StoreHouse.Application.InventoryItems.Queries;
using Usm.Shared.BuildingBlocks.Validation;

namespace StoreHouse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddStoreHouseApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });
        services.AddAssemblyValidators<GetInventoryItemsQueryHandler>();
        return services;
    }
}
