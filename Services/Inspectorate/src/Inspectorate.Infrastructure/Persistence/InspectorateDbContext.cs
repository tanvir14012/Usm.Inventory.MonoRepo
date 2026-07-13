using Microsoft.EntityFrameworkCore;
using Usm.Shared.Data.DbContextExtensions;
using Inspectorate.Domain.Inspections;

namespace Inspectorate.Infrastructure.Persistence;

public class InspectorateDbContext(DbContextOptions<InspectorateDbContext> options)
    : ServiceDbContext(options, "inspectorate")
{
    public DbSet<Inspection> Inspections => Set<Inspection>();
}
