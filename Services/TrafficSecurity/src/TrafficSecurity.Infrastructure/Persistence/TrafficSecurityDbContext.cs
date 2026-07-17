using Microsoft.EntityFrameworkCore;
using TrafficSecurity.Application.Abstractions;
using Usm.Shared.Data.DbContextExtensions;
using TrafficSecurity.Domain.VehicleSafetyRecords;

namespace TrafficSecurity.Infrastructure.Persistence;

public class TrafficSecurityDbContext(DbContextOptions<TrafficSecurityDbContext> options)
    : ServiceDbContext(options, "trafficsecurity"), ITrafficSecurityDbContext
{
    public DbSet<VehicleSafetyRecord> VehicleSafetyRecords => Set<VehicleSafetyRecord>();
}
