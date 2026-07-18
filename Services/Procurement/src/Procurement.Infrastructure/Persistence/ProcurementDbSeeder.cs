using Microsoft.EntityFrameworkCore;
using Procurement.Domain.PurchaseOrders;
using Procurement.Domain.Suppliers;
using Usm.Shared.Contracts.Localization;

namespace Procurement.Infrastructure.Persistence;

public static class ProcurementDbSeeder
{
    public static async Task SeedAsync(ProcurementDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.PurchaseOrders.AnyAsync(cancellationToken))
        {
            return;
        }

        var supplierA = Supplier.Create(
            new LocalizedText(En: "Patriot Defense Systems", Ar: "أنظمة الدفاع باتريوت"),
            "ops@patriot.example");
        var supplierB = Supplier.Create(
            new LocalizedText(En: "Atlantic Field Logistics", Ar: "الأطلسي للإمداد الميداني"),
            "ops@atlantic.example");

        var po1 = PurchaseOrder.Create("PO-OPS-2401", supplierA.Id, DateTimeOffset.UtcNow.AddDays(-12), DateTimeOffset.UtcNow.AddDays(5));
        po1.SetSupplier(supplierA);
        po1.SetTotalAmount(1250000m);
        po1.Submit();
        po1.Approve();

        var po2 = PurchaseOrder.Create("PO-OPS-2402", supplierB.Id, DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow.AddDays(2));
        po2.SetSupplier(supplierB);
        po2.SetTotalAmount(248000m);
        po2.Submit();

        var po3 = PurchaseOrder.Create("PO-OPS-2403", supplierA.Id, DateTimeOffset.UtcNow.AddDays(-20), DateTimeOffset.UtcNow.AddDays(-1));
        po3.SetSupplier(supplierA);
        po3.SetTotalAmount(789500m);
        po3.Submit();
        po3.Approve();
        po3.MarkDelivered();

        dbContext.Suppliers.AddRange(supplierA, supplierB);
        dbContext.PurchaseOrders.AddRange(po1, po2, po3);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
