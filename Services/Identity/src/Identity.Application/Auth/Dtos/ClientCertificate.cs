namespace Identity.Application.Auth.Dtos;

public sealed record ClientCertificate(
    string Subject,
    string Issuer,
    string Thumbprint,
    byte[] RawData);