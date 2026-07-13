using Iam.Application.Roles.Dtos;
using MediatR;

namespace Iam.Application.Roles.Queries;

public sealed record GetRolesQuery : IRequest<IReadOnlyList<RoleDto>>;
