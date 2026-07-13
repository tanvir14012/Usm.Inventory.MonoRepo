using Administration.Application.Departments.Dtos;
using MediatR;

namespace Administration.Application.Departments.Queries;

public sealed class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    public Task<IReadOnlyList<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<DepartmentDto> result = [];
        return Task.FromResult(result);
    }
}
