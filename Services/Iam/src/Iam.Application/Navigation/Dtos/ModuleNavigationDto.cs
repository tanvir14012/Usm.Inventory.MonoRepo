using Iam.Domain.Navigation;

namespace Iam.Application.Navigation.Dtos;

public sealed record SidebarMenuItemDto(
    Guid Id,
    Guid? ParentSidebarMenuItemId,
    string SystemName,
    string MenuId,
    string LocalizedName,
    int DisplayOrder,
    string MaterialIconName,
    bool IsActive,
    IReadOnlyList<SidebarMenuItemDto> Children);

public sealed record ModuleNavigationDto(
    Guid Id,
    MilitaryBuildingBlockType BuildingBlockType,
    string SystemName,
    string MenuId,
    string LocalizedName,
    int DisplayOrder,
    string MaterialIconName,
    bool IsActive,
    IReadOnlyList<SidebarMenuItemDto> SidebarItems);
