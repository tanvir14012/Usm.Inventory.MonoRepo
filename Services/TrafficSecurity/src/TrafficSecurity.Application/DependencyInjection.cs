using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.Validation;
using TrafficSecurity.Application.VehicleSafetyRecords.Queries;

namespace TrafficSecurity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTrafficSecurityApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });
        services.AddAssemblyValidators<GetVehicleSafetyRecordsQueryHandler>();
        return services;
    }
}
