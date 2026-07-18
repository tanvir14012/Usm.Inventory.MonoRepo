using Procurement.Domain.Common;
using Procurement.Domain.PurchaseOrders;
using Usm.Shared.Contracts.Localization;
using Usm.Shared.Data.DbContextExtensions;

namespace Procurement.Domain.Suppliers;

public sealed class Supplier : AggregateRoot<Guid>, IAuditable
{
    public LocalizedText Name { get; private set; } = LocalizedText.Empty;
    public string ContactEmail { get; private set; } = string.Empty;
    public string? ContactPhone { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public ICollection<PurchaseOrder> PurchaseOrders { get; private set; } = new List<PurchaseOrder>();

    private Supplier() { }

    public static Supplier Create(LocalizedText name, string contactEmail, string? contactPhone = null, string? address = null)
    {
        return new Supplier
        {
            Id = Guid.NewGuid(),
            Name = name,
            ContactEmail = contactEmail,
            ContactPhone = contactPhone,
            Address = address,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
