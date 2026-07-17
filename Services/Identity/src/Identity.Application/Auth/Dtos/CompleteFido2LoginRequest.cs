using Fido2NetLib;

namespace Identity.Application.Auth.Dtos;

public sealed record CompleteFido2LoginRequest(
    AuthenticatorAssertionRawResponse AssertionResponse,
    string AssertionOptionsJson);
