using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHouse.Domain.InventoryItems;

namespace StoreHouse.Infrastructure.Persistence.Configurations;

internal sealed class InventoryItemEntityConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Unit)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
