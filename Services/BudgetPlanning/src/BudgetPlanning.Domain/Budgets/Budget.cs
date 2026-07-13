using BudgetPlanning.Domain.Common;
using Usm.Shared.Contracts.Localization;
using Usm.Shared.Data.DbContextExtensions;

namespace BudgetPlanning.Domain.Budgets;

public enum BudgetStatus { Draft, Active, Closed, Cancelled }

public sealed class Budget : AggregateRoot<Guid>, IAuditable
{
    public LocalizedText Title { get; private set; } = LocalizedText.Empty;
    public int FiscalYear { get; private set; }
    public Guid DepartmentId { get; private set; }
    public decimal TotalAllocated { get; private set; }
    public decimal TotalSpent { get; private set; }
    public BudgetStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Budget() { }

    public static Budget Create(LocalizedText title, int fiscalYear, Guid departmentId, decimal totalAllocated)
    {
        return new Budget
        {
            Id = Guid.NewGuid(),
            Title = title,
            FiscalYear = fiscalYear,
            DepartmentId = departmentId,
            TotalAllocated = totalAllocated,
            TotalSpent = 0m,
            Status = BudgetStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Activate() => Status = BudgetStatus.Active;

    public void AddExpenditure(decimal amount)
    {
        if (TotalSpent + amount > TotalAllocated)
            throw new InvalidOperationException("Expenditure exceeds the total allocated budget.");
        TotalSpent += amount;
    }
}
