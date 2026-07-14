using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.Data.DbContextExtensions;

namespace Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options): ServiceDbContext(options, "identity"), IIdentityDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<UserCredential> UserCredentials => Set<UserCredential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.UseOpenIddict();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await base.SaveChangesAsync(ct);
    }
}
