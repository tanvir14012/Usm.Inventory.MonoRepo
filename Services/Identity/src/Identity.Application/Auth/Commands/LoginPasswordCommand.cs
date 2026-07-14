using Identity.Application.Users.Dtos;
using MediatR;

namespace Identity.Application.Auth.Commands
{
    public sealed record LoginPasswordCommand(string Username, string Password) : IRequest<AuthenticatedUser?>;
}
