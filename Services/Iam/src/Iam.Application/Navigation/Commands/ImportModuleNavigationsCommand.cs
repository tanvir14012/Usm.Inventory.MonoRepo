using Iam.Application.Abstractions;
using Iam.Application.Common;
using Iam.Application.Navigation.Dtos;
using Iam.Domain.Navigation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Iam.Application.Navigation.Commands;

public sealed record ImportModuleNavigationsCommand(
    ImportModuleNavigationsInput Input) : IRequest<IReadOnlyList<ModuleNavigationDto>>;

public sealed class ImportModuleNavigationsCommandHandler(IIamDbContext dbContext)
    : IRequestHandler<ImportModuleNavigationsCommand, IReadOnlyList<ModuleNavigationDto>>
{
    public async Task<IReadOnlyList<ModuleNavigationDto>> Handle(ImportModuleNavigationsCommand request, CancellationToken cancellationToken)
    {
        if (request.Input.Modules.Count == 0)
        {
            throw new ApplicationValidationException("At least one module navigation item is required.");
        }

        EnsureUnique(
            request.Input.Modules.Select(x => x.MenuId),
            "module menu id");

        var existingForBuildingBlock = await dbContext.ModuleNavigations
            .Include(x => x.SidebarItems)
            .Where(x => x.BuildingBlockType == request.Input.BuildingBlockType)
            .ToListAsync(cancellationToken);

        if (existingForBuildingBlock.Count > 0)
        {
            dbContext.ModuleNavigations.RemoveRange(existingForBuildingBlock);
        }

        foreach (var moduleInput in request.Input.Modules.OrderBy(x => x.DisplayOrder))
        {
            var module = ModuleNavigation.Create(
                request.Input.BuildingBlockType,
                moduleInput.SystemName,
                moduleInput.MenuId,
                moduleInput.LocalizedName,
                moduleInput.DisplayOrder,
                moduleInput.MaterialIconName,
                moduleInput.IsActive);

            module.ReplaceSidebarItems(ModuleNavigationBuilder.BuildSidebar(module.Id, moduleInput, request.Input.UseDerivedSidebarWhenEmpty));
            dbContext.ModuleNavigations.Add(module);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var imported = await dbContext.ModuleNavigations
            .Include(x => x.SidebarItems)
            .AsNoTracking()
            .Where(x => x.BuildingBlockType == request.Input.BuildingBlockType)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);

        return imported.Select(NavigationMapping.ToDto).ToList();
    }

    private static void EnsureUnique(IEnumerable<string?> values, string label)
        => ModuleNavigationBuilder.EnsureUnique(values, label);
}

internal static class ModuleNavigationBuilder
{
    internal static IReadOnlyList<SidebarMenuItem> BuildSidebar(
        Guid moduleId,
        ModuleNavigationInput moduleInput,
        bool useDerivedSidebarWhenEmpty)
    {
        var rawSidebar = moduleInput.SidebarItems ?? [];
        if (rawSidebar.Count == 0 && useDerivedSidebarWhenEmpty)
        {
            rawSidebar = UsMilitaryNavigationDefaults
                .ForModule(moduleInput.SystemName, moduleInput.MenuId)
                .Select(x => new SidebarMenuItemInput(
                    null,
                    null,
                    x.SystemName,
                    x.MenuId,
                    x.LocalizedName,
                    x.DisplayOrder,
                    x.MaterialIconName,
                    x.IsActive,
                    []))
                .ToList();
        }

        var result = new List<SidebarMenuItem>();
        AddSidebarLevel(moduleId, null, rawSidebar, result);
        EnsureUnique(result.Select(x => x.MenuId), "sidebar menu id");
        return result;
    }

    internal static void EnsureUnique(IEnumerable<string?> values, string label)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ApplicationValidationException($"A required {label} is missing.");
            }

            if (!seen.Add(normalized))
            {
                throw new ApplicationValidationException($"Duplicate {label} '{normalized}' was found.");
            }
        }
    }

    private static void AddSidebarLevel(
        Guid moduleId,
        Guid? parentId,
        IReadOnlyList<SidebarMenuItemInput> source,
        ICollection<SidebarMenuItem> target)
    {
        foreach (var item in source.OrderBy(x => x.DisplayOrder))
        {
            var entity = SidebarMenuItem.Create(
                moduleId,
                parentId,
                item.SystemName,
                item.MenuId,
                item.LocalizedName,
                item.DisplayOrder,
                item.MaterialIconName,
                item.IsActive);

            target.Add(entity);

            var children = item.Children ?? [];
            if (children.Count > 0)
            {
                AddSidebarLevel(moduleId, entity.Id, children, target);
            }
        }
    }
}
