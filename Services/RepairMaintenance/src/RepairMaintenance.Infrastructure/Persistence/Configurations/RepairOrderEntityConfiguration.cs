using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairMaintenance.Domain.RepairOrders;

namespace RepairMaintenance.Infrastructure.Persistence.Configurations;

internal sealed class RepairOrderEntityConfiguration : IEntityTypeConfiguration<RepairOrder>
{
    public void Configure(EntityTypeBuilder<RepairOrder> builder)
    {
        builder.ToTable("repair_orders", "repairmaintenance");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1024)
            .IsRequired();

        builder.HasIndex(x => x.OrderNumber).IsUnique();
    }
}
