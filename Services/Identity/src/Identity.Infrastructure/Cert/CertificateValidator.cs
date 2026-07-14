using Identity.Application.Auth.Dtos;
using Identity.Application.Auth.Utils;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Usm.Shared.Reflection.AssemblyScanning.ServiceLifetimeMarkers;

namespace Identity.Infrastructure.Cert;


public sealed class CertificateValidator : ICertificateValidator, ISingletonService
{
    public bool Validate(ClientCertificate certificate)
    {
        if (certificate.RawData is null || certificate.RawData.Length == 0)
            return false;

        using var cert = new X509Certificate2(certificate.RawData);

        //
        // Validity period
        //
        var now = DateTimeOffset.UtcNow;

        if (now < cert.NotBefore)
            return false;

        if (now > cert.NotAfter)
            return false;

        //
        // Must contain Client Authentication EKU
        //
        const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";

        var ekuExtension = cert.Extensions
            .OfType<X509EnhancedKeyUsageExtension>()
            .FirstOrDefault();

        if (ekuExtension is null)
            return false;

        var hasClientAuth = ekuExtension.EnhancedKeyUsages
            .Cast<Oid>()
            .Any(x => x.Value == ClientAuthenticationOid);

        if (!hasClientAuth)
            return false;

        //
        // Digital Signature usage
        //
        var keyUsage = cert.Extensions
            .OfType<X509KeyUsageExtension>()
            .FirstOrDefault();

        if (keyUsage is not null &&
            !keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature))
        {
            return false;
        }

        return true;
    }
}