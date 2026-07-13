using Iam.Application.Roles.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.Validation;

namespace Iam.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIamApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });
        services.AddAssemblyValidators<GetRolesQueryHandler>();
        return services;
    }
}
