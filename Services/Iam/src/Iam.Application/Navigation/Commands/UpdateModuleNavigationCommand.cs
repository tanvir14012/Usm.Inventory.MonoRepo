using Iam.Application.Abstractions;
using Iam.Application.Common;
using Iam.Application.Navigation.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Iam.Application.Navigation.Commands;

public sealed record UpdateModuleNavigationCommand(
    Guid ModuleId,
    ModuleNavigationInput Module) : IRequest<ModuleNavigationDto>;

public sealed class UpdateModuleNavigationCommandHandler(IIamDbContext dbContext)
    : IRequestHandler<UpdateModuleNavigationCommand, ModuleNavigationDto>
{
    public async Task<ModuleNavigationDto> Handle(UpdateModuleNavigationCommand request, CancellationToken cancellationToken)
    {
        var module = await dbContext.ModuleNavigations
            .Include(x => x.SidebarItems)
            .FirstOrDefaultAsync(x => x.Id == request.ModuleId, cancellationToken);

        if (module is null)
        {
            throw new ApplicationValidationException("Module navigation item was not found.");
        }

        var duplicateExists = await dbContext.ModuleNavigations.AnyAsync(
            x => x.Id != request.ModuleId &&
                 x.BuildingBlockType == module.BuildingBlockType &&
                 x.MenuId == request.Module.MenuId,
            cancellationToken);

        if (duplicateExists)
        {
            throw new ApplicationValidationException(
                $"Menu id '{request.Module.MenuId}' already exists for '{module.BuildingBlockType}'.");
        }

        module.Update(
            request.Module.SystemName,
            request.Module.MenuId,
            request.Module.LocalizedName,
            request.Module.DisplayOrder,
            request.Module.MaterialIconName,
            request.Module.IsActive);

        module.ReplaceSidebarItems(
            ModuleNavigationBuilder.BuildSidebar(module.Id, request.Module, useDerivedSidebarWhenEmpty: true));

        await dbContext.SaveChangesAsync(cancellationToken);
        return NavigationMapping.ToDto(module);
    }
}
