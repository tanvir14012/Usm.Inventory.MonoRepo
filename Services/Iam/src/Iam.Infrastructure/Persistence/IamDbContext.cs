using Iam.Domain.Permissions;
using Iam.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.Data.DbContextExtensions;

namespace Iam.Infrastructure.Persistence;

public sealed class IamDbContext(DbContextOptions<IamDbContext> options)
    : ServiceDbContext(options, "iam")
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
}
