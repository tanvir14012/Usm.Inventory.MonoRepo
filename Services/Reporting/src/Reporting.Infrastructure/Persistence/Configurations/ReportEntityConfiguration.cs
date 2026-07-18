using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reporting.Domain.Reports;

namespace Reporting.Infrastructure.Persistence.Configurations;

internal sealed class ReportEntityConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReportType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Parameters)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.OutputPath)
            .HasMaxLength(1024);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(x => x.GeneratedById);
        builder.HasIndex(x => x.Status);
    }
}
