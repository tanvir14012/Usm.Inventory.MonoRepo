namespace Iam.Application.Authorization;

public static class PermissionPolicies
{
    public const string Prefix = "perm:";

    public static string Permission(string permissionCode) => $"{Prefix}{permissionCode}";
}
