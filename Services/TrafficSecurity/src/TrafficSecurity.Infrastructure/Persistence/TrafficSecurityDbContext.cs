using Microsoft.EntityFrameworkCore;
using Usm.Shared.Data.DbContextExtensions;
using TrafficSecurity.Domain.VehicleSafetyRecords;

namespace TrafficSecurity.Infrastructure.Persistence;

public class TrafficSecurityDbContext(DbContextOptions<TrafficSecurityDbContext> options)
    : ServiceDbContext(options, "trafficsecurity")
{
    public DbSet<VehicleSafetyRecord> VehicleSafetyRecords => Set<VehicleSafetyRecord>();
}
