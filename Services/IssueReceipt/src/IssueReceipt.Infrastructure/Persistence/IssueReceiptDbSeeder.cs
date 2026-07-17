using IssueReceipt.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace IssueReceipt.Infrastructure.Persistence;

public static class IssueReceiptDbSeeder
{
    public static async Task SeedAsync(IssueReceiptDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.IssueTransactions.AnyAsync(cancellationToken) || await dbContext.ReceiptTransactions.AnyAsync(cancellationToken))
        {
            return;
        }

        var warehouseId = Guid.NewGuid();
        var rationId = Guid.NewGuid();
        var batteryId = Guid.NewGuid();

        dbContext.ReceiptTransactions.AddRange(
            ReceiptTransaction.Create("RCV-2401", rationId, warehouseId, 200, "Theater Supply Hub", DateTimeOffset.UtcNow.AddDays(-6), "Emergency replenishment"),
            ReceiptTransaction.Create("RCV-2402", batteryId, warehouseId, 75, "Signals Depot", DateTimeOffset.UtcNow.AddDays(-2), "Priority convoy"));

        dbContext.IssueTransactions.AddRange(
            IssueTransaction.Create("ISS-2401", rationId, warehouseId, 120, "3rd Brigade Support Company", DateTimeOffset.UtcNow.AddDays(-4), "Field exercise"),
            IssueTransaction.Create("ISS-2402", batteryId, warehouseId, 30, "Air Defense Radar Team", DateTimeOffset.UtcNow.AddDays(-1), "Night operations"));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
