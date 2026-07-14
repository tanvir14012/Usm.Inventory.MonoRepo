using DocumentShare.Domain.Common;
using Usm.Shared.Contracts.Localization;
using Usm.Shared.Data.DbContextExtensions;

namespace DocumentShare.Domain.Documents;

public class Document : AggregateRoot<Guid>, IAuditable
{
    public LocalizedText Title { get; private set; } = default!;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public Guid UploadedById { get; private set; }
    public bool IsPublic { get; private set; }
    public string Tags { get; private set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Document() { }

    public static Document Create(
        LocalizedText title,
        string fileName,
        string contentType,
        long fileSizeBytes,
        string storagePath,
        Guid uploadedById,
        bool isPublic)
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            Title = title,
            FileName = fileName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            StoragePath = storagePath,
            UploadedById = uploadedById,
            IsPublic = isPublic
        };
    }

    public void MakePublic() => IsPublic = true;

    public void MakePrivate() => IsPublic = false;
}
