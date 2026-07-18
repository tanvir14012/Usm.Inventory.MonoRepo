using BudgetPlanning.Domain.BudgetItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetPlanning.Infrastructure.Persistence.Configurations;

internal sealed class BudgetItemEntityConfiguration : IEntityTypeConfiguration<BudgetItem>
{
    public void Configure(EntityTypeBuilder<BudgetItem> builder)
    {
        builder.ToTable("budget_items", "budgetplanning");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
            .HasMaxLength(1024)
            .IsRequired();

        builder.HasIndex(x => x.BudgetId);
    }
}
