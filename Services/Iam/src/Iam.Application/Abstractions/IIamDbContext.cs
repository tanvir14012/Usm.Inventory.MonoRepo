using Iam.Domain.Organograms;
using Iam.Domain.Permissions;
using Iam.Domain.Roles;
using Microsoft.EntityFrameworkCore;

namespace Iam.Application.Abstractions;

public interface IIamDbContext
{
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }

    DbSet<OrganogramTemplate> OrganogramTemplates { get; }
    DbSet<TemplateDepartment> TemplateDepartments { get; }
    DbSet<TemplateBuildingBlock> TemplateBuildingBlocks { get; }
    DbSet<TemplateRoleDefinition> TemplateRoles { get; }
    DbSet<TemplatePermissionDefinition> TemplatePermissions { get; }
    DbSet<TemplateRolePermission> TemplateRolePermissions { get; }
    DbSet<TemplateRoleHierarchy> TemplateRoleHierarchies { get; }
    DbSet<TemplatePositionAllocation> TemplatePositionAllocations { get; }
    DbSet<TemplateApprovalPath> TemplateApprovalPaths { get; }

    DbSet<OrganogramInstance> OrganogramInstances { get; }
    DbSet<OrganizationalUnit> OrganizationalUnits { get; }
    DbSet<InstanceBuildingBlock> InstanceBuildingBlocks { get; }
    DbSet<InstanceRoleDefinition> InstanceRoles { get; }
    DbSet<InstancePermissionDefinition> InstancePermissions { get; }
    DbSet<InstanceRolePermission> InstanceRolePermissions { get; }
    DbSet<InstanceRoleHierarchy> InstanceRoleHierarchies { get; }
    DbSet<InstancePositionAllocation> InstancePositionAllocations { get; }
    DbSet<InstanceApprovalPath> InstanceApprovalPaths { get; }
    DbSet<UserOrganogramAssignment> UserOrganogramAssignments { get; }
    DbSet<SuperAdminAssignment> SuperAdminAssignments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
