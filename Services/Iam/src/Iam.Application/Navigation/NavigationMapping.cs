using Iam.Application.Navigation.Dtos;
using Iam.Domain.Navigation;

namespace Iam.Application.Navigation;

internal static class NavigationMapping
{
    public static ModuleNavigationDto ToDto(ModuleNavigation moduleNavigation)
    {
        var sidebarByParent = moduleNavigation.SidebarItems
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.LocalizedName)
            .GroupBy(x => x.ParentSidebarMenuItemId)
            .ToDictionary(x => x.Key, x => x.ToList());

        IReadOnlyList<SidebarMenuItemDto> BuildChildren(Guid? parentId)
        {
            if (!sidebarByParent.TryGetValue(parentId, out var rows))
            {
                return [];
            }

            return rows
                .Select(x => new SidebarMenuItemDto(
                    x.Id,
                    x.ParentSidebarMenuItemId,
                    x.SystemName,
                    x.MenuId,
                    x.LocalizedName,
                    x.DisplayOrder,
                    x.MaterialIconName,
                    x.IsActive,
                    BuildChildren(x.Id)))
                .ToList();
        }

        return new ModuleNavigationDto(
            moduleNavigation.Id,
            moduleNavigation.BuildingBlockType,
            moduleNavigation.SystemName,
            moduleNavigation.MenuId,
            moduleNavigation.LocalizedName,
            moduleNavigation.DisplayOrder,
            moduleNavigation.MaterialIconName,
            moduleNavigation.IsActive,
            BuildChildren(null));
    }
}
