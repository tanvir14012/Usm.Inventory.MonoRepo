using Iam.Application.Abstractions;
using Iam.Domain.Organograms;
using Iam.Domain.Permissions;
using Iam.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.Data.DbContextExtensions;

namespace Iam.Infrastructure.Persistence;

public sealed class IamDbContext(DbContextOptions<IamDbContext> options)
    : ServiceDbContext(options, "iam"), IIamDbContext
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<OrganogramTemplate> OrganogramTemplates => Set<OrganogramTemplate>();
    public DbSet<TemplateDepartment> TemplateDepartments => Set<TemplateDepartment>();
    public DbSet<TemplateBuildingBlock> TemplateBuildingBlocks => Set<TemplateBuildingBlock>();
    public DbSet<TemplateRoleDefinition> TemplateRoles => Set<TemplateRoleDefinition>();
    public DbSet<TemplatePermissionDefinition> TemplatePermissions => Set<TemplatePermissionDefinition>();
    public DbSet<TemplateRolePermission> TemplateRolePermissions => Set<TemplateRolePermission>();
    public DbSet<TemplateRoleHierarchy> TemplateRoleHierarchies => Set<TemplateRoleHierarchy>();
    public DbSet<TemplatePositionAllocation> TemplatePositionAllocations => Set<TemplatePositionAllocation>();
    public DbSet<TemplateApprovalPath> TemplateApprovalPaths => Set<TemplateApprovalPath>();

    public DbSet<OrganogramInstance> OrganogramInstances => Set<OrganogramInstance>();
    public DbSet<OrganizationalUnit> OrganizationalUnits => Set<OrganizationalUnit>();
    public DbSet<InstanceBuildingBlock> InstanceBuildingBlocks => Set<InstanceBuildingBlock>();
    public DbSet<InstanceRoleDefinition> InstanceRoles => Set<InstanceRoleDefinition>();
    public DbSet<InstancePermissionDefinition> InstancePermissions => Set<InstancePermissionDefinition>();
    public DbSet<InstanceRolePermission> InstanceRolePermissions => Set<InstanceRolePermission>();
    public DbSet<InstanceRoleHierarchy> InstanceRoleHierarchies => Set<InstanceRoleHierarchy>();
    public DbSet<InstancePositionAllocation> InstancePositionAllocations => Set<InstancePositionAllocation>();
    public DbSet<InstanceApprovalPath> InstanceApprovalPaths => Set<InstanceApprovalPath>();
    public DbSet<UserOrganogramAssignment> UserOrganogramAssignments => Set<UserOrganogramAssignment>();
    public DbSet<SuperAdminAssignment> SuperAdminAssignments => Set<SuperAdminAssignment>();
}
