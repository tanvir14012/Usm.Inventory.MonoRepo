using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Procurement.Application.PurchaseOrders.Queries;
using Usm.Shared.BuildingBlocks.Validation;

namespace Procurement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddProcurementApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });
        services.AddAssemblyValidators<GetPurchaseOrdersQuery>();
        return services;
    }
}
