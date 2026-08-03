using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Usm.Shared.Infrastructure.CDN.Abstractions;
using Usm.Shared.Infrastructure.CDN.Models;
using Usm.Shared.Infrastructure.CDN.Options;

namespace Usm.Shared.Infrastructure.CDN.Security;

/// <summary>
/// Generates and validates Nginx <c>ngx_http_secure_link_module</c> compatible signed URLs.
///
/// Nginx secure_link_md5 MD5 input format:
///   Without IP binding: "{expires}{uri} {secret}"
///   With IP binding:    "{expires}{uri}{remoteAddr} {secret}"
///
/// These formats must EXACTLY match the nginx.conf <c>secure_link_md5</c> directive.
/// See the bundled nginx/nginx.conf for the corresponding server configuration.
///
/// Shell equivalent (without IP):
///   echo -n "{expires}{uri} {secret}" | openssl md5 -binary | openssl base64 | tr +/ -_ | tr -d =
/// </summary>
internal sealed class NginxSecureLinkGenerator(IOptions<CdnOptions> options) : INginxSecureLinkGenerator
{
    private readonly NginxSecureLinkOptions _opts = options.Value.SecureLink;

    public SecureLinkToken Generate(string uri, string? remoteAddr = null, TimeSpan? expiry = null)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(expiry ?? _opts.DefaultExpiry);
        var expiresUnix = expiresAt.ToUnixTimeSeconds();
        var hash = ComputeHash(uri, expiresUnix, _opts.BindToClientIp ? remoteAddr : null);

        return new SecureLinkToken
        {
            Hash = hash,
            ExpiresAt = expiresUnix,
            Uri = uri,
            BoundToIp = _opts.BindToClientIp ? remoteAddr : null
        };
    }

    public bool Validate(string uri, string token, long expires, string? remoteAddr = null)
    {
        // Reject already-expired tokens before doing any crypto work
        if (expires < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            return false;

        var expected = ComputeHash(uri, expires, _opts.BindToClientIp ? remoteAddr : null);

        // Constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(token));
    }

    public string BuildSignedUrl(string baseUrl, string uri, string? remoteAddr = null, TimeSpan? expiry = null)
    {
        var token = Generate(uri, remoteAddr, expiry);
        var sep = uri.Contains('?') ? '&' : '?';
        var url = $"{baseUrl.TrimEnd('/')}{uri}{sep}md5={token.Hash}&expires={token.ExpiresAt}";

        // Return a new token with the fully-assembled URL
        return new SecureLinkToken
        {
            Hash = token.Hash,
            ExpiresAt = token.ExpiresAt,
            Uri = uri,
            BoundToIp = token.BoundToIp,
            SignedUrl = url
        }.SignedUrl;
    }

    // ── Hash computation ─────────────────────────────────────────────────────

    /// <summary>
    /// Builds the MD5 input string exactly as Nginx does and returns the base64url-encoded hash.
    ///
    ///   Without IP: "{expires}{uri} {secret}"
    ///   With IP:    "{expires}{uri}{remoteAddr} {secret}"
    ///
    /// Note: there is NO separator between uri and remoteAddr – this matches the Nginx convention
    /// where variables are concatenated directly (see the nginx docs example: "2147483647/s/link127.0.0.1 secret").
    /// </summary>
    private string ComputeHash(string uri, long expires, string? remoteAddr)
    {
        var input = remoteAddr is not null
            ? $"{expires}{uri}{remoteAddr} {_opts.SecretKey}"
            : $"{expires}{uri} {_opts.SecretKey}";

        // MD5 of raw UTF-8 bytes → 16-byte binary digest → base64url without padding
        Span<byte> digest = stackalloc byte[16];
        MD5.HashData(Encoding.UTF8.GetBytes(input), digest);

        return Convert.ToBase64String(digest)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
