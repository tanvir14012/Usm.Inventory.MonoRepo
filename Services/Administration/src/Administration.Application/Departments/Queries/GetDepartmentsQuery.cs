using Administration.Application.Departments.Dtos;
using MediatR;

namespace Administration.Application.Departments.Queries;

public sealed record GetDepartmentsQuery : IRequest<IReadOnlyList<DepartmentDto>>;
