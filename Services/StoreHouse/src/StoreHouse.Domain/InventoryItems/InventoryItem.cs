using StoreHouse.Domain.Common;
using Usm.Shared.Contracts.Localization;
using Usm.Shared.Data.DbContextExtensions;

namespace StoreHouse.Domain.InventoryItems;

public sealed class InventoryItem : AggregateRoot<Guid>, IAuditable
{
    private InventoryItem() { }

    public LocalizedText Name { get; private set; } = LocalizedText.Empty;
    public string Code { get; private set; } = string.Empty;
    public LocalizedText Description { get; private set; } = LocalizedText.Empty;
    public string Unit { get; private set; } = string.Empty;
    public decimal CurrentQuantity { get; private set; }
    public decimal ReorderLevel { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public static InventoryItem Create(LocalizedText name, string code, string unit, decimal reorderLevel)
    {
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            Unit = unit,
            ReorderLevel = reorderLevel,
            CurrentQuantity = 0,
            IsActive = true
        };
        item.RaiseDomainEvent(new InventoryItemCreatedDomainEvent(item.Id));
        return item;
    }

    public void AdjustQuantity(decimal delta)
    {
        CurrentQuantity += delta;
        RaiseDomainEvent(new InventoryQuantityAdjustedDomainEvent(Id, delta, CurrentQuantity));
    }
}

public sealed record InventoryItemCreatedDomainEvent(Guid InventoryItemId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public sealed record InventoryQuantityAdjustedDomainEvent(Guid InventoryItemId, decimal Delta, decimal NewQuantity) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
