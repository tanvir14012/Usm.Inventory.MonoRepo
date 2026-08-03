using DocumentShare.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.Data.DbContextExtensions;

namespace DocumentShare.Infrastructure.Persistence;

public class DocumentShareDbContext(DbContextOptions<DocumentShareDbContext> options)
    : ServiceDbContext(options, "documentshare")
{
    public DbSet<Document> Documents => Set<Document>();
}
