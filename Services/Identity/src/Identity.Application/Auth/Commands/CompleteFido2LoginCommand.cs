using Fido2NetLib;
using Identity.Application.Users.Dtos;
using MediatR;

namespace Identity.Application.Auth.Commands;

public sealed record CompleteFido2LoginCommand(
    AuthenticatorAssertionRawResponse AssertionResponse,
    string AssertionOptionsJson)
    : IRequest<AuthenticatedUser?>;
