using Iam.Application.Authorization;
using Iam.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Usm.Shared.Caching.Abstractions;
using Usm.Shared.Caching.Models;

namespace Iam.Infrastructure.Authorization;

public sealed record PermissionRequirement(string PermissionCode) : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler(
    IamDbContext dbContext,
    ICacheService cacheService)
    : AuthorizationHandler<PermissionRequirement>
{
    private static readonly CacheEntryOptions PermissionSnapshotCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        SlidingExpiration = TimeSpan.FromMinutes(1)
    };

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

        var instanceId = TryGetInstanceId(context.User);
        var snapshot = await cacheService.GetOrCreateAsync(
            PermissionCacheKeys.Snapshot(userId.Value, instanceId),
            token => LoadSnapshotAsync(dbContext, userId.Value, instanceId, token),
            PermissionSnapshotCacheOptions);

        if (snapshot.IsSuperAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        if (HasPermissionClaim(context.User, requirement.PermissionCode))
        {
            context.Succeed(requirement);
            return;
        }

        if (snapshot.HasPermission(requirement.PermissionCode))
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

    private static async Task<PermissionSnapshot> LoadSnapshotAsync(
        IamDbContext dbContext,
        Guid userId,
        Guid? instanceId,
        CancellationToken cancellationToken)
    {
        var isSuperAdmin = await dbContext.SuperAdminAssignments
            .AnyAsync(x => x.UserId == userId, cancellationToken);

        if (isSuperAdmin)
        {
            return PermissionSnapshot.SuperAdmin;
        }

        var permissions = await dbContext.UserOrganogramAssignments
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .Where(x => !instanceId.HasValue || x.InstanceId == instanceId.Value)
            .Join(
                dbContext.InstanceRolePermissions.AsNoTracking(),
                assignment => new { assignment.InstanceId, assignment.RoleCode },
                rolePermission => new { rolePermission.InstanceId, rolePermission.RoleCode },
                (_, rolePermission) => rolePermission.PermissionCode)
            .Distinct()
            .ToListAsync(cancellationToken);

        return PermissionSnapshot.FromPermissions(permissions);
    }

    private sealed class PermissionSnapshot
    {
        public static PermissionSnapshot SuperAdmin { get; } = new() { IsSuperAdmin = true };

        public bool IsSuperAdmin { get; init; }
        public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();

        public static PermissionSnapshot FromPermissions(IEnumerable<string> permissions)
        {
            return new PermissionSnapshot
            {
                Permissions = permissions
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            };
        }

        public bool HasPermission(string permissionCode)
        {
            return Permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
        }
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
