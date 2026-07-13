using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RepairMaintenance.Application.RepairOrders.Queries;
using Usm.Shared.BuildingBlocks.Validation;

namespace RepairMaintenance.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddRepairMaintenanceApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });
        services.AddAssemblyValidators<GetRepairOrdersQueryHandler>();
        return services;
    }
}
