using Microsoft.EntityFrameworkCore;
using TrafficSecurity.Application.Abstractions;
using TrafficSecurity.Domain.VehicleSafetyRecords;
using Usm.Shared.Data.DbContextExtensions;

namespace TrafficSecurity.Infrastructure.Persistence;

public class TrafficSecurityDbContext(DbContextOptions<TrafficSecurityDbContext> options)
    : ServiceDbContext(options, "trafficsecurity"), ITrafficSecurityDbContext
{
    public DbSet<VehicleSafetyRecord> VehicleSafetyRecords => Set<VehicleSafetyRecord>();
}
