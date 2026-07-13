using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.Validation;

namespace Communication.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCommunicationApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });
        services.AddAssemblyValidators<DependencyInjection>();
        return services;
    }
}
