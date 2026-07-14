using Identity.Domain.Common;
using System.Text.Json;
using Usm.Shared.Data.DbContextExtensions;

namespace Identity.Domain.Users
{
    public enum CredentialType
    {
        Password = 1,
        Cac = 2,
        Fido2 = 3
    }
    public sealed class UserCredential : AggregateRoot<Guid>, IAuditable
    {
        public Guid UserId { get; private set; }

        public CredentialType Type { get; private set; }

        // Username or DoD ID or CredentialId
        public string Identifier { get; private set; } = string.Empty;

        // JsonDocument serialized to string. PasswordHash or FIDO2 metadata. Empty object for CAC.
        public string Metadata { get; private set; } = "{}";

        public string? DisplayName { get; private set; }

        public bool IsEnabled { get; private set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }

        public static UserCredential Create(
            Guid userId,
            CredentialType type,
            string identifier,
            string metadata,
            string? displayName = null)
        {
            return new UserCredential
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Identifier = identifier,
                Metadata = metadata,
                DisplayName = displayName,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        public T GetMetadata<T>() => JsonSerializer.Deserialize<T>(Metadata)!;

    }

    public sealed record PasswordCredentialMetadata(string PasswordHash);
    public sealed record CacCredentialMetadata();
    public sealed record Fido2CredentialMetadata(
        byte[] CredentialId,
        byte[] PublicKey,
        uint SignatureCounter,
        Guid AaGuid,
        string[] Transports,
        bool UserVerified);
}
