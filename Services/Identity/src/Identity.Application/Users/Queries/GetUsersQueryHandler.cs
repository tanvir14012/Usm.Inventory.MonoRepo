using Identity.Application.Users.Dtos;
using MediatR;

namespace Identity.Application.Users.Queries;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<UserDto> result = [];
        return Task.FromResult(result);
    }
}
