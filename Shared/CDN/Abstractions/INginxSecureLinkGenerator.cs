using Usm.Shared.Infrastructure.CDN.Models;

namespace Usm.Shared.Infrastructure.CDN.Abstractions;

/// <summary>
/// Generates and validates Nginx ngx_http_secure_link_module compatible signed URLs.
///
/// Hash format (must exactly match the nginx.conf <c>secure_link_md5</c> directive):
///   Without IP: base64url(MD5("{expires}{uri} {secret}"))
///   With IP:    base64url(MD5("{expires}{uri}{remoteAddr} {secret}"))
/// </summary>
public interface INginxSecureLinkGenerator
{
    /// <summary>Generates a secure token for the given URI.</summary>
    /// <param name="uri">The path portion of the URL (no query string).</param>
    /// <param name="remoteAddr">Client IP for IP-bound tokens. Pass null to skip IP binding.</param>
    /// <param name="expiry">Token lifetime. Falls back to <c>NginxSecureLinkOptions.DefaultExpiry</c>.</param>
    SecureLinkToken Generate(string uri, string? remoteAddr = null, TimeSpan? expiry = null);

    /// <summary>Validates a token received from a client.</summary>
    bool Validate(string uri, string token, long expires, string? remoteAddr = null);

    /// <summary>Builds a complete signed URL appending <c>?md5=…&amp;expires=…</c> to the base URL.</summary>
    string BuildSignedUrl(string baseUrl, string uri, string? remoteAddr = null, TimeSpan? expiry = null);
}
