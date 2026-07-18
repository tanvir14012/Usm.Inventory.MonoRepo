using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHouse.Domain.Warehouses;

namespace StoreHouse.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseEntityConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("warehouses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Location)
            .HasMaxLength(512);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
