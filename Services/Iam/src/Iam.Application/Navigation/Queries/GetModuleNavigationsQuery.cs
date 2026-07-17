using Iam.Application.Abstractions;
using Iam.Application.Navigation.Dtos;
using Iam.Domain.Navigation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Iam.Application.Navigation.Queries;

public sealed record GetModuleNavigationsQuery(
    MilitaryBuildingBlockType? BuildingBlockType) : IRequest<IReadOnlyList<ModuleNavigationDto>>;

public sealed class GetModuleNavigationsQueryHandler(IIamDbContext dbContext)
    : IRequestHandler<GetModuleNavigationsQuery, IReadOnlyList<ModuleNavigationDto>>
{
    public async Task<IReadOnlyList<ModuleNavigationDto>> Handle(GetModuleNavigationsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.ModuleNavigations
            .Include(x => x.SidebarItems)
            .AsNoTracking()
            .AsQueryable();

        if (request.BuildingBlockType.HasValue)
        {
            query = query.Where(x => x.BuildingBlockType == request.BuildingBlockType.Value);
        }

        var modules = await query
            .OrderBy(x => x.BuildingBlockType)
            .ThenBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);

        return modules.Select(NavigationMapping.ToDto).ToList();
    }
}
