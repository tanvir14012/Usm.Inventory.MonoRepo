using Microsoft.EntityFrameworkCore;
using Salvage.Application.Abstractions;
using Salvage.Domain.SalvageRecords;
using Usm.Shared.Data.DbContextExtensions;

namespace Salvage.Infrastructure.Persistence;

public class SalvageDbContext(DbContextOptions<SalvageDbContext> options)
    : ServiceDbContext(options, "salvage"), ISalvageDbContext
{
    public DbSet<SalvageRecord> SalvageRecords => Set<SalvageRecord>();
}
