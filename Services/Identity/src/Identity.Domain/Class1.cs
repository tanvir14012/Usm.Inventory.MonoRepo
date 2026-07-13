using Usm.Shared.Contracts.Localization;
using Usm.Shared.Data.DbContextExtensions;

namespace Identity.Domain;

public sealed class LoginDevice : IMultiTenant, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public LocalizedText DisplayName { get; set; } = LocalizedText.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsDeleted { get; private set; }

    public void SoftDelete() => IsDeleted = true;
}
