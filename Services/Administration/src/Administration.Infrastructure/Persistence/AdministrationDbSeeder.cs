using Administration.Domain.Departments;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.Contracts.Localization;

namespace Administration.Infrastructure.Persistence;

public static class AdministrationDbSeeder
{
    public static async Task SeedAsync(AdministrationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Departments.AnyAsync(cancellationToken))
        {
            return;
        }

        var logistics = Department.Create(
            new LocalizedText(En: "Logistics Command", Ar: "قيادة الإمداد"),
            "LOGCOM");
        var procurement = Department.Create(
            new LocalizedText(En: "Procurement Directorate", Ar: "مديرية المشتريات"),
            "PROC",
            logistics.Id);
        var maintenance = Department.Create(
            new LocalizedText(En: "Maintenance Battalion", Ar: "كتيبة الصيانة"),
            "MAINT",
            logistics.Id);
        var security = Department.Create(
            new LocalizedText(En: "Traffic Security Cell", Ar: "خلية أمن المرور"),
            "TRFSEC",
            logistics.Id);

        dbContext.Departments.AddRange(logistics, procurement, maintenance, security);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
