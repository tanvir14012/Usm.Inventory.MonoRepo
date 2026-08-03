using Inspectorate.Domain.Inspections;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.Data.DbContextExtensions;

namespace Inspectorate.Infrastructure.Persistence;

public class InspectorateDbContext(DbContextOptions<InspectorateDbContext> options)
    : ServiceDbContext(options, "inspectorate")
{
    public DbSet<Inspection> Inspections => Set<Inspection>();
}
