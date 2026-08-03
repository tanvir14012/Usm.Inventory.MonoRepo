namespace Usm.Shared.Infrastructure.CDN.Options;

/// <summary>
/// Options for Nginx ngx_http_secure_link_module compatible MD5 signed URL generation.
///
/// The MD5 input string that must exactly match the nginx.conf secure_link_md5 directive:
///
///   Without IP binding: "{expires}{uri} {secret}"
///     nginx directive:  secure_link_md5 "$secure_link_expires$uri $secret_key_value";
///
///   With IP binding:    "{expires}{uri}{remoteAddr} {secret}"
///     nginx directive:  secure_link_md5 "$secure_link_expires$uri$remote_addr $secret_key_value";
///
/// The hash is computed as: base64url(MD5(inputString)) with no padding.
/// </summary>
public sealed class NginxSecureLinkOptions
{
    /// <summary>Shared secret embedded in the MD5 hash. Must match the value in nginx.conf.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Default link expiry. The expiration timestamp is encoded as a Unix epoch (seconds).</summary>
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// When true, the client IP address is included in the MD5 input, binding the token to a specific IP.
    /// The nginx.conf secure_link_md5 directive MUST match this setting.
    /// </summary>
    public bool BindToClientIp { get; set; } = false;
}
