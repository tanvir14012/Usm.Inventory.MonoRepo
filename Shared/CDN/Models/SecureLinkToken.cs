namespace Usm.Shared.Infrastructure.CDN.Models;

/// <summary>A generated Nginx-compatible secure link token with its expiry and signed URL.</summary>
public sealed record SecureLinkToken
{
    /// <summary>base64url(MD5(input)) without padding – the md5 query parameter value.</summary>
    public required string Hash { get; init; }

    /// <summary>Unix epoch seconds at which the link expires.</summary>
    public required long ExpiresAt { get; init; }

    /// <summary>The URI path (without query string) that was signed.</summary>
    public required string Uri { get; init; }

    /// <summary>IP address bound to this token when BindToClientIp is true; otherwise null.</summary>
    public string? BoundToIp { get; init; }

    /// <summary>Full signed URL: "{baseUrl}{uri}?md5={Hash}&amp;expires={ExpiresAt}".</summary>
    public string SignedUrl { get; init; } = string.Empty;
}
