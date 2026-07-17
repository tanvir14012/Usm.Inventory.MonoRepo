using Microsoft.EntityFrameworkCore;
using TrafficSecurity.Domain.VehicleSafetyRecords;

namespace TrafficSecurity.Infrastructure.Persistence;

public static class TrafficSecurityDbSeeder
{
    public static async Task SeedAsync(TrafficSecurityDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.VehicleSafetyRecords.AnyAsync(cancellationToken))
        {
            return;
        }

        var passed = VehicleSafetyRecord.Create("HMMWV-312", Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-5), Guid.NewGuid());
        passed.Pass(DateTimeOffset.UtcNow.AddDays(25), "Convoy-ready");

        var attention = VehicleSafetyRecord.Create("FMTV-118", Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-2), Guid.NewGuid());
        attention.RequiresAttention("Brake pressure trending low", DateTimeOffset.UtcNow.AddDays(3));

        var failed = VehicleSafetyRecord.Create("MRAP-207", Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1), Guid.NewGuid());
        failed.Fail("Steering rack leak detected");

        dbContext.VehicleSafetyRecords.AddRange(passed, attention, failed);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
