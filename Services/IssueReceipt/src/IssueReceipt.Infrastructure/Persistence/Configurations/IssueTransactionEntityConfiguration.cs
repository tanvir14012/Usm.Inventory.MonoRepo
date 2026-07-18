using IssueReceipt.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueReceipt.Infrastructure.Persistence.Configurations;

internal sealed class IssueTransactionEntityConfiguration : IEntityTypeConfiguration<IssueTransaction>
{
    public void Configure(EntityTypeBuilder<IssueTransaction> builder)
    {
        builder.ToTable("issue_transactions", "issuereceipt");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(x => x.TransactionNumber).IsUnique();
    }
}
