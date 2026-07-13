using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.Validation;
using DocumentShare.Application.Documents.Queries;

namespace DocumentShare.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDocumentShareApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });
        services.AddAssemblyValidators<GetDocumentsQueryHandler>();
        return services;
    }
}
