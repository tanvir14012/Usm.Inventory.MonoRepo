using BudgetPlanning.Domain.BudgetItems;
using BudgetPlanning.Domain.Budgets;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.Data.DbContextExtensions;

namespace BudgetPlanning.Infrastructure.Persistence;

public sealed class BudgetPlanningDbContext(DbContextOptions<BudgetPlanningDbContext> options)
    : ServiceDbContext(options, "budgetplanning")
{
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetItem> BudgetItems => Set<BudgetItem>();
}
