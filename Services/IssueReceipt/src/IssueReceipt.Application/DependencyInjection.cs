using IssueReceipt.Application.Transactions.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.Validation;

namespace IssueReceipt.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIssueReceiptApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });
        services.AddAssemblyValidators<GetTransactionsQueryHandler>();
        return services;
    }
}
