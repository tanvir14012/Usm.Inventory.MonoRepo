using Administration.Domain.Departments;
using Microsoft.EntityFrameworkCore;

namespace Administration.Application.Abstractions;

public interface IAdministrationDbContext
{
    DbSet<Department> Departments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
