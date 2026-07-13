using BudgetPlanning.Domain.Common;

namespace BudgetPlanning.Domain.BudgetItems;

public sealed class BudgetItem : EntityBase<Guid>
{
    public Guid BudgetId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal AllocatedAmount { get; private set; }
    public decimal SpentAmount { get; private set; }

    private BudgetItem() { }

    public static BudgetItem Create(Guid budgetId, string description, decimal allocatedAmount)
    {
        return new BudgetItem
        {
            Id = Guid.NewGuid(),
            BudgetId = budgetId,
            Description = description,
            AllocatedAmount = allocatedAmount,
            SpentAmount = 0m
        };
    }
}
