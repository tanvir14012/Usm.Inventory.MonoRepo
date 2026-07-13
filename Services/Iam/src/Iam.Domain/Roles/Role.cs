using Iam.Domain.Common;
using Iam.Domain.Permissions;
using Usm.Shared.Data.DbContextExtensions;

namespace Iam.Domain.Roles;

public sealed record RoleCreatedDomainEvent(Guid RoleId, string Name) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public sealed class Role : AggregateRoot<Guid>, IAuditable
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    private readonly List<Permission> _permissions = [];
    public IReadOnlyList<Permission> Permissions => _permissions.AsReadOnly();
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Role() { }

    public static Role Create(string name, string description)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        role.RaiseDomainEvent(new RoleCreatedDomainEvent(role.Id, name));
        return role;
    }
}
