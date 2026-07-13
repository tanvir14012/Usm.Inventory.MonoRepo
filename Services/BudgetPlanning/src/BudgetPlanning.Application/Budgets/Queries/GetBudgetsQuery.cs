using MediatR;

namespace BudgetPlanning.Application.Budgets.Queries;

public sealed record GetBudgetsQuery : IRequest<IReadOnlyList<BudgetDto>>;
