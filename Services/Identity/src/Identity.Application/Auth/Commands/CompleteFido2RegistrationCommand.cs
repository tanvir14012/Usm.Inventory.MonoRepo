using Fido2NetLib;
using MediatR;

namespace Identity.Application.Auth.Commands;

public sealed record CompleteFido2RegistrationCommand(
    AuthenticatorAttestationRawResponse AttestationResponse)
    : IRequest;