using Iam.Domain.Common;
using Usm.Shared.Data.DbContextExtensions;

namespace Iam.Domain.Navigation;

public enum MilitaryBuildingBlockType
{
    Headquarters = 1,
    SubDepot = 2,
    Branch = 3,
    Directorate = 4,
    Group = 5,
    Cell = 6,
    Section = 7,
    Division = 8,
    Brigade = 9,
    Battalion = 10,
    Regiment = 11,
    Company = 12,
    Platoon = 13,
    Squadron = 14,
    Troop = 15,
    Battery = 16
}

public sealed class ModuleNavigation : AggregateRoot<Guid>, IAuditable
{
    private readonly List<SidebarMenuItem> _sidebarItems = [];

    public MilitaryBuildingBlockType BuildingBlockType { get; private set; }
    public string SystemName { get; private set; } = string.Empty;
    public string MenuId { get; private set; } = string.Empty;
    public string LocalizedName { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public string MaterialIconName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<SidebarMenuItem> SidebarItems => _sidebarItems.AsReadOnly();

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    private ModuleNavigation() { }

    public static ModuleNavigation Create(
        MilitaryBuildingBlockType buildingBlockType,
        string systemName,
        string menuId,
        string localizedName,
        int displayOrder,
        string materialIconName,
        bool isActive)
    {
        return new ModuleNavigation
        {
            Id = Guid.NewGuid(),
            BuildingBlockType = buildingBlockType,
            SystemName = systemName.Trim(),
            MenuId = menuId.Trim(),
            LocalizedName = localizedName.Trim(),
            DisplayOrder = displayOrder,
            MaterialIconName = materialIconName.Trim(),
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string systemName,
        string menuId,
        string localizedName,
        int displayOrder,
        string materialIconName,
        bool isActive)
    {
        SystemName = systemName.Trim();
        MenuId = menuId.Trim();
        LocalizedName = localizedName.Trim();
        DisplayOrder = displayOrder;
        MaterialIconName = materialIconName.Trim();
        IsActive = isActive;
    }

    public void ReplaceSidebarItems(IEnumerable<SidebarMenuItem> sidebarItems)
    {
        _sidebarItems.Clear();
        _sidebarItems.AddRange(sidebarItems);
    }
}

public sealed class SidebarMenuItem : EntityBase<Guid>
{
    public Guid ModuleNavigationId { get; private set; }
    public Guid? ParentSidebarMenuItemId { get; private set; }
    public string SystemName { get; private set; } = string.Empty;
    public string MenuId { get; private set; } = string.Empty;
    public string LocalizedName { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public string MaterialIconName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private SidebarMenuItem() { }

    public static SidebarMenuItem Create(
        Guid moduleNavigationId,
        Guid? parentSidebarMenuItemId,
        string systemName,
        string menuId,
        string localizedName,
        int displayOrder,
        string materialIconName,
        bool isActive)
    {
        return new SidebarMenuItem
        {
            Id = Guid.NewGuid(),
            ModuleNavigationId = moduleNavigationId,
            ParentSidebarMenuItemId = parentSidebarMenuItemId,
            SystemName = systemName.Trim(),
            MenuId = menuId.Trim(),
            LocalizedName = localizedName.Trim(),
            DisplayOrder = displayOrder,
            MaterialIconName = materialIconName.Trim(),
            IsActive = isActive
        };
    }
}
