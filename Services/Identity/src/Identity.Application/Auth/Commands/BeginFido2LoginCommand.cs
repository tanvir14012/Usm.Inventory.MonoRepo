using MediatR;

namespace Identity.Application.Auth.Commands;

public sealed record BeginFido2LoginCommand
    : IRequest<string>;