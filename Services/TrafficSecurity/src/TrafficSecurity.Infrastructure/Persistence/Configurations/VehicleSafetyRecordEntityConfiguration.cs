using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrafficSecurity.Domain.VehicleSafetyRecords;

namespace TrafficSecurity.Infrastructure.Persistence.Configurations;

internal sealed class VehicleSafetyRecordEntityConfiguration : IEntityTypeConfiguration<VehicleSafetyRecord>
{
    public void Configure(EntityTypeBuilder<VehicleSafetyRecord> builder)
    {
        builder.ToTable("vehicle_safety_records");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VehicleRegistrationNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Remarks)
            .HasMaxLength(1024);

        builder.HasIndex(x => x.VehicleId);
        builder.HasIndex(x => x.Status);
    }
}
