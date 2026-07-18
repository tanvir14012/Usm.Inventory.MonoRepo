using IssueReceipt.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueReceipt.Infrastructure.Persistence.Configurations;

internal sealed class ReceiptTransactionEntityConfiguration : IEntityTypeConfiguration<ReceiptTransaction>
{
    public void Configure(EntityTypeBuilder<ReceiptTransaction> builder)
    {
        builder.ToTable("receipt_transactions", "issuereceipt");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(x => x.TransactionNumber).IsUnique();
    }
}
