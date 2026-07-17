using Fido2NetLib;

namespace Identity.Application.Auth.Dtos;

public sealed record CompleteFido2RegistrationRequest(
    AuthenticatorAttestationRawResponse AttestationResponse,
    string AttestationOptionsJson);