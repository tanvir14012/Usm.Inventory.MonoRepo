using Microsoft.EntityFrameworkCore;
using StoreHouse.Domain.InventoryItems;
using StoreHouse.Domain.Warehouses;
using Usm.Shared.Contracts.Localization;

namespace StoreHouse.Infrastructure.Persistence;

public static class StoreHouseDbSeeder
{
    public static async Task SeedAsync(StoreHouseDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.InventoryItems.AnyAsync(cancellationToken))
        {
            return;
        }

        var warehouse = Warehouse.Create(
            new LocalizedText(En: "Forward Support Warehouse", Ar: "مستودع الإسناد الأمامي"),
            "FSW-01",
            "Sector Echo");

        var rationKit = InventoryItem.Create(
            new LocalizedText(En: "MRE Ration Kit", Ar: "حقيبة إعاشة"),
            "MRE-24",
            "box",
            120);
        rationKit.AdjustQuantity(340);

        var batteryPack = InventoryItem.Create(
            new LocalizedText(En: "Radio Battery Pack", Ar: "بطارية جهاز لاسلكي"),
            "BAT-RDO",
            "ea",
            80);
        batteryPack.AdjustQuantity(58);

        var tireSet = InventoryItem.Create(
            new LocalizedText(En: "Convoy Tire Set", Ar: "طقم إطارات القافلة"),
            "TIRE-CV",
            "set",
            24);
        tireSet.AdjustQuantity(19);

        dbContext.Warehouses.Add(warehouse);
        dbContext.InventoryItems.AddRange(rationKit, batteryPack, tireSet);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
