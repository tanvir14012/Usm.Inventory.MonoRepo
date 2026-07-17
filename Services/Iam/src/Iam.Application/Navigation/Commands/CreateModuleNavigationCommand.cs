using Iam.Application.Abstractions;
using Iam.Application.Common;
using Iam.Application.Navigation.Dtos;
using Iam.Domain.Navigation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Iam.Application.Navigation.Commands;

public sealed record CreateModuleNavigationCommand(
    MilitaryBuildingBlockType BuildingBlockType,
    ModuleNavigationInput Module) : IRequest<ModuleNavigationDto>;

public sealed class CreateModuleNavigationCommandHandler(IIamDbContext dbContext)
    : IRequestHandler<CreateModuleNavigationCommand, ModuleNavigationDto>
{
    public async Task<ModuleNavigationDto> Handle(CreateModuleNavigationCommand request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.ModuleNavigations.AnyAsync(
            x => x.BuildingBlockType == request.BuildingBlockType && x.MenuId == request.Module.MenuId,
            cancellationToken);

        if (exists)
        {
            throw new ApplicationValidationException(
                $"Menu id '{request.Module.MenuId}' already exists for '{request.BuildingBlockType}'.");
        }

        var module = ModuleNavigation.Create(
            request.BuildingBlockType,
            request.Module.SystemName,
            request.Module.MenuId,
            request.Module.LocalizedName,
            request.Module.DisplayOrder,
            request.Module.MaterialIconName,
            request.Module.IsActive);

        module.ReplaceSidebarItems(
            ModuleNavigationBuilder.BuildSidebar(module.Id, request.Module, useDerivedSidebarWhenEmpty: true));

        dbContext.ModuleNavigations.Add(module);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NavigationMapping.ToDto(module);
    }
}
