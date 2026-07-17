using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procurement.Domain.Suppliers;
using Usm.Shared.Data.DbContextExtensions;

namespace Procurement.Infrastructure.Persistence.Configurations;

internal sealed class SupplierEntityConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasJsonbLocalization()
            .IsRequired();

        builder.Property(x => x.ContactEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ContactPhone)
            .HasMaxLength(32);

        builder.Property(x => x.Address)
            .HasMaxLength(512);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.ContactEmail);
    }
}
