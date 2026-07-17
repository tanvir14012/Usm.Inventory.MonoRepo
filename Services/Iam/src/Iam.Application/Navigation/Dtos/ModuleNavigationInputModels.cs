using Iam.Domain.Navigation;

namespace Iam.Application.Navigation.Dtos;

public sealed record SidebarMenuItemInput(
    Guid? Id,
    Guid? ParentSidebarMenuItemId,
    string SystemName,
    string MenuId,
    string LocalizedName,
    int DisplayOrder,
    string MaterialIconName,
    bool IsActive,
    IReadOnlyList<SidebarMenuItemInput>? Children);

public sealed record ModuleNavigationInput(
    Guid? Id,
    string SystemName,
    string MenuId,
    string LocalizedName,
    int DisplayOrder,
    string MaterialIconName,
    bool IsActive,
    IReadOnlyList<SidebarMenuItemInput>? SidebarItems);

public sealed record ImportModuleNavigationsInput(
    MilitaryBuildingBlockType BuildingBlockType,
    IReadOnlyList<ModuleNavigationInput> Modules,
    bool UseDerivedSidebarWhenEmpty = true);
