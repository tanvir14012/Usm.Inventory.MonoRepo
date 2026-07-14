
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

public interface IIdentityDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserCredential> UserCredentials { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);

}
