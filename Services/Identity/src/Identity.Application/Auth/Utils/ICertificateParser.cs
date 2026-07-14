using Identity.Application.Auth.Dtos;

namespace Identity.Application.Auth.Utils;

public interface ICertificateParser
{
    string GetDodId(ClientCertificate certificate);
}