using DocumentShare.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentShare.Infrastructure.Persistence.Configurations;

internal sealed class DocumentEntityConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.StoragePath)
            .HasMaxLength(1024)
            .IsRequired();

        builder.HasIndex(x => x.UploadedById);
        builder.HasIndex(x => x.IsPublic);
    }
}
