using Microsoft.EntityFrameworkCore;
using TrafficSecurity.Domain.VehicleSafetyRecords;

namespace TrafficSecurity.Application.Abstractions;

public interface ITrafficSecurityDbContext
{
    DbSet<VehicleSafetyRecord> VehicleSafetyRecords { get; }
}
