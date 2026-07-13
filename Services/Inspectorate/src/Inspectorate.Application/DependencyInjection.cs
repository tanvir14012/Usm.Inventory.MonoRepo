using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.Validation;
using Inspectorate.Application.Inspections.Queries;

namespace Inspectorate.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddInspectorateApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });
        services.AddAssemblyValidators<GetInspectionsQueryHandler>();
        return services;
    }
}
