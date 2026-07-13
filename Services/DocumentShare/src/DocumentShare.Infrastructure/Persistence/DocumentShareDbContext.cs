using Microsoft.EntityFrameworkCore;
using Usm.Shared.Data.DbContextExtensions;
using DocumentShare.Domain.Documents;

namespace DocumentShare.Infrastructure.Persistence;

public class DocumentShareDbContext(DbContextOptions<DocumentShareDbContext> options)
    : ServiceDbContext(options, "documentshare")
{
    public DbSet<Document> Documents => Set<Document>();
}
