using Identity.Application.Auth.Dtos;

namespace Identity.Application.Auth.Utils;

public interface ICertificateValidator
{
    bool Validate(ClientCertificate certificate);
}