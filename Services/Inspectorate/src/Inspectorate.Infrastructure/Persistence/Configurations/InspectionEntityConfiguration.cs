using Inspectorate.Domain.Inspections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inspectorate.Infrastructure.Persistence.Configurations;

internal sealed class InspectionEntityConfiguration : IEntityTypeConfiguration<Inspection>
{
    public void Configure(EntityTypeBuilder<Inspection> builder)
    {
        builder.ToTable("inspections");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InspectionNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.SubjectType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Findings)
            .HasMaxLength(2048);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(x => x.InspectionNumber).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ScheduledDate);
    }
}
