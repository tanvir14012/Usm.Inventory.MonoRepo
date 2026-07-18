using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salvage.Domain.SalvageRecords;

namespace Salvage.Infrastructure.Persistence.Configurations;

internal sealed class SalvageRecordEntityConfiguration : IEntityTypeConfiguration<SalvageRecord>
{
    public void Configure(EntityTypeBuilder<SalvageRecord> builder)
    {
        builder.ToTable("salvage_records", "salvage");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecordNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(x => x.ApprovedBy)
            .HasMaxLength(128);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(x => x.RecordNumber).IsUnique();
        builder.HasIndex(x => x.Status);
    }
}
