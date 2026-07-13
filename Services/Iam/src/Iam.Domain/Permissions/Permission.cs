using Iam.Domain.Common;
using Usm.Shared.Data.DbContextExtensions;

namespace Iam.Domain.Permissions;

public sealed class Permission : EntityBase<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;

    private Permission() { }

    public static Permission Create(string name, string resource, string action)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Name = name,
            Resource = resource,
            Action = action
        };
    }
}
