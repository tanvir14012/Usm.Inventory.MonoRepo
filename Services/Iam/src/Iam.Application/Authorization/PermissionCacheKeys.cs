namespace Iam.Application.Authorization;

public static class PermissionCacheKeys
{
    public static string Snapshot(Guid userId, Guid? instanceId)
    {
        var instanceSegment = instanceId?.ToString("N") ?? "all";
        return $"iam:authz:snapshot:user:{userId:N}:instance:{instanceSegment}";
    }

    public static string UserPattern(Guid userId)
    {
        return $"iam:authz:snapshot:user:{userId:N}:*";
    }
}
