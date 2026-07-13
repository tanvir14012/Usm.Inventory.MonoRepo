using Identity.Application.Users.Dtos;
using MediatR;

namespace Identity.Application.Users.Queries;

public sealed record GetUsersQuery : IRequest<IReadOnlyList<UserDto>>;
