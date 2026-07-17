using Administration.Application.Abstractions;
using Administration.Application.Departments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.EntityFramework.Caching.Extensions;

namespace Administration.Application.Departments.Queries;

public sealed class GetDepartmentsQueryHandler(IAdministrationDbContext context)
    : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    public async Task<IReadOnlyList<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var items = await context.Departments
            .AsNoTracking()
            .OrderBy(x => x.Name.En)
            .CacheAsync("department-list", cancellationToken: cancellationToken);

        return items
            .Select(x => new DepartmentDto(
                x.Id,
                x.Name.En,
                x.Name.Ar,
                x.Code,
                x.ParentId,
                x.IsActive,
                x.CreatedAt))
            .ToArray();
    }
}
