using BudgetPlanning.Domain.Budgets;

namespace BudgetPlanning.Application.Budgets.Queries;

public sealed record BudgetDto(
    Guid Id,
    int FiscalYear,
    Guid DepartmentId,
    decimal TotalAllocated,
    decimal TotalSpent,
    BudgetStatus Status);
