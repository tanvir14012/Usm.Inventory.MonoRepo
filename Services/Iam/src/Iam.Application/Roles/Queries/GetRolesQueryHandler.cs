using Iam.Application.Roles.Dtos;
using MediatR;

namespace Iam.Application.Roles.Queries;

public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<RoleDto> result = [];
        return Task.FromResult(result);
    }
}
