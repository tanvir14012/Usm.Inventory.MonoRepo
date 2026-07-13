using MediatR;

namespace BudgetPlanning.Application.Budgets.Queries;

public sealed class GetBudgetsQueryHandler : IRequestHandler<GetBudgetsQuery, IReadOnlyList<BudgetDto>>
{
    public Task<IReadOnlyList<BudgetDto>> Handle(GetBudgetsQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<BudgetDto>>(Array.Empty<BudgetDto>());
    }
}
