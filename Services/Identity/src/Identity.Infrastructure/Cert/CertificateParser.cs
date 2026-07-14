using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates;
using Identity.Application.Auth.Dtos;
using Identity.Application.Auth.Utils;
using Usm.Shared.Reflection.AssemblyScanning.ServiceLifetimeMarkers;

namespace Identity.Infrastructure.Cert;

public sealed class CertificateParser : ICertificateParser, ISingletonService
{
    private const string SubjectAlternativeNameOid = "2.5.29.17";
    private const string DodIdPrefix = "dodid:";

    public string GetDodId(ClientCertificate certificate)
    {
        using var cert = new X509Certificate2(certificate.RawData);

        //
        // Look for Subject Alternative Name
        //
        var sanExtension = cert.Extensions
            .Cast<X509Extension>()
            .FirstOrDefault(x => x.Oid?.Value == SubjectAlternativeNameOid);

        if (sanExtension is not null)
        {
            var dodId = TryReadDodIdFromSan(sanExtension);

            if (!string.IsNullOrWhiteSpace(dodId))
                return dodId;
        }

        //
        // Fallback: Subject CN (useful for development certificates)
        //
        var cn = cert.GetNameInfo(X509NameType.SimpleName, false);

        if (!string.IsNullOrWhiteSpace(cn) &&
            cn.StartsWith(DodIdPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return cn[DodIdPrefix.Length..];
        }

        throw new InvalidOperationException(
            "DoD ID (EDIPI) was not found in the client certificate.");
    }

    private static string? TryReadDodIdFromSan(X509Extension extension)
    {
        var reader = new AsnReader(extension.RawData, AsnEncodingRules.DER);
        var sequence = reader.ReadSequence();

        while (sequence.HasData)
        {
            var tag = sequence.PeekTag();

            //
            // GeneralName -> uniformResourceIdentifier [6]
            //
            if (tag.TagClass == TagClass.ContextSpecific &&
                tag.TagValue == 6)
            {
                var uri = sequence.ReadCharacterString(
                    UniversalTagNumber.IA5String,
                    new Asn1Tag(TagClass.ContextSpecific, 6));

                if (uri.StartsWith(DodIdPrefix, StringComparison.OrdinalIgnoreCase))
                    return uri[DodIdPrefix.Length..];
            }
            else
            {
                sequence.ReadEncodedValue();
            }
        }

        return null;
    }
}