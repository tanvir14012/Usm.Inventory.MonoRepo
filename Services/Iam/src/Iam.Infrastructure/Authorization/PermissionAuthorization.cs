using Iam.Application.Authorization;
using Iam.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Iam.Infrastructure.Authorization;

public sealed record PermissionRequirement(string PermissionCode) : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler(IamDbContext dbContext)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (IsSuperAdminByClaim(context.User))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = TryGetUserId(context.User);
        if (userId is null)
        {
            return;
        }

        var isSuperAdmin = await dbContext.SuperAdminAssignments.AnyAsync(x => x.UserId == userId.Value);
        if (isSuperAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        if (HasPermissionClaim(context.User, requirement.PermissionCode))
        {
            context.Succeed(requirement);
            return;
        }

        var instanceId = TryGetInstanceId(context.User);

        var hasPermission = await dbContext.UserOrganogramAssignments
            .Where(x => x.UserId == userId.Value && x.IsActive)
            .Where(x => !instanceId.HasValue || x.InstanceId == instanceId.Value)
            .Join(
                dbContext.InstanceRolePermissions,
                assignment => new { assignment.InstanceId, assignment.RoleCode },
                rolePermission => new { rolePermission.InstanceId, rolePermission.RoleCode },
                (assignment, rolePermission) => rolePermission)
            .AnyAsync(x => x.PermissionCode == requirement.PermissionCode);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }

    private static bool IsSuperAdminByClaim(ClaimsPrincipal principal)
    {
        return principal.Claims.Any(x =>
            (x.Type is ClaimTypes.Role or "role") &&
            x.Value.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasPermissionClaim(ClaimsPrincipal principal, string permissionCode)
    {
        return principal.Claims.Any(x =>
            (x.Type is "permission" or "permissions" or "scope") &&
            x.Value.Contains(permissionCode, StringComparison.OrdinalIgnoreCase));
    }

    private static Guid? TryGetUserId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue("sub");
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    private static Guid? TryGetInstanceId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue("instance_id");
        return Guid.TryParse(raw, out var instanceId) ? instanceId : null;
    }
}

public sealed class PermissionAuthorizationPolicyProvider(
    IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PermissionPolicies.Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return base.GetPolicyAsync(policyName);
        }

        var permissionCode = policyName[PermissionPolicies.Prefix.Length..];
        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionRequirement(permissionCode))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
