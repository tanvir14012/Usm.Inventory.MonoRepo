using Administration.Application.Departments.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.Validation;

namespace Administration.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAdministrationApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });
        services.AddAssemblyValidators<GetDepartmentsQueryHandler>();
        return services;
    }
}
