using StoreHouse.Domain.Common;
using Usm.Shared.Contracts.Localization;
using Usm.Shared.Data.DbContextExtensions;

namespace StoreHouse.Domain.Warehouses;

public sealed class Warehouse : AggregateRoot<Guid>, IAuditable
{
    private Warehouse() { }

    public LocalizedText Name { get; private set; } = LocalizedText.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Location { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public static Warehouse Create(LocalizedText name, string code, string? location = null)
    {
        return new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            Location = location,
            IsActive = true
        };
    }
}
