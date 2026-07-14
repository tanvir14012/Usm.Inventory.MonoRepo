using MediatR;

namespace Identity.Application.Auth.Commands;

public sealed record BeginFido2RegistrationCommand(Guid UserId)
    : IRequest<string>;
