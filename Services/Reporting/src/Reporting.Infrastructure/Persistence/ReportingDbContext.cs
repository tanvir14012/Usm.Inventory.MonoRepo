using Microsoft.EntityFrameworkCore;
using Reporting.Domain.Reports;
using Usm.Shared.Data.DbContextExtensions;

namespace Reporting.Infrastructure.Persistence;

public sealed class ReportingDbContext(DbContextOptions<ReportingDbContext> options)
    : ServiceDbContext(options, "reporting")
{
    public DbSet<Report> Reports => Set<Report>();
}
