using BudgetPlanning.Application.Budgets.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.Validation;

namespace BudgetPlanning.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddBudgetPlanningApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });
        services.AddAssemblyValidators<GetBudgetsQuery>();
        return services;
    }
}
