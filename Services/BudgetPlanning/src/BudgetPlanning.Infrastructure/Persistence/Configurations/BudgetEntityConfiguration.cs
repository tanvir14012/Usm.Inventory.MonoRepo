using BudgetPlanning.Domain.Budgets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetPlanning.Infrastructure.Persistence.Configurations;

internal sealed class BudgetEntityConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets", "budgetplanning");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(x => new { x.DepartmentId, x.FiscalYear });
        builder.HasIndex(x => x.Status);
    }
}
