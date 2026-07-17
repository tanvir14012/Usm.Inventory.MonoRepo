using Administration.Application.Abstractions;
using Administration.Domain.Departments;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.Data.DbContextExtensions;

namespace Administration.Infrastructure.Persistence;

public sealed class AdministrationDbContext(DbContextOptions<AdministrationDbContext> options)
    : ServiceDbContext(options, "administration"), IAdministrationDbContext
{
    public DbSet<Department> Departments => Set<Department>();
}
