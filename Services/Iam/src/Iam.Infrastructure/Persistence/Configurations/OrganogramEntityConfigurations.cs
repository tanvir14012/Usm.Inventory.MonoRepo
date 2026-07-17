using Iam.Domain.Organograms;
using Iam.Domain.Permissions;
using Iam.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iam.Infrastructure.Persistence.Configurations;

internal sealed class RoleEntityConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(512);
    }
}

internal sealed class PermissionEntityConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Resource).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
    }
}

internal sealed class OrganogramTemplateConfiguration : IEntityTypeConfiguration<OrganogramTemplate>
{
    public void Configure(EntityTypeBuilder<OrganogramTemplate> builder)
    {
        builder.ToTable("organogram_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Version).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.HasIndex(x => new { x.Name, x.Version }).IsUnique();

        builder.HasMany(x => x.Departments).WithOne().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.BuildingBlocks).WithOne().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Roles).WithOne().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Permissions).WithOne().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.RolePermissions).WithOne().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.RoleHierarchies).WithOne().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.PositionAllocations).WithOne().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.ApprovalPaths).WithOne().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TemplateDepartmentConfiguration : IEntityTypeConfiguration<TemplateDepartment>
{
    public void Configure(EntityTypeBuilder<TemplateDepartment> builder)
    {
        builder.ToTable("template_departments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DepartmentCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ParentDepartmentCode).HasMaxLength(80);
        builder.HasIndex(x => new { x.TemplateId, x.DepartmentCode }).IsUnique();
    }
}

internal sealed class TemplateBuildingBlockConfiguration : IEntityTypeConfiguration<TemplateBuildingBlock>
{
    public void Configure(EntityTypeBuilder<TemplateBuildingBlock> builder)
    {
        builder.ToTable("template_building_blocks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BuildingBlockCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DepartmentCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ParentBuildingBlockCode).HasMaxLength(80);
        builder.HasIndex(x => new { x.TemplateId, x.BuildingBlockCode }).IsUnique();
    }
}

internal sealed class TemplateRoleDefinitionConfiguration : IEntityTypeConfiguration<TemplateRoleDefinition>
{
    public void Configure(EntityTypeBuilder<TemplateRoleDefinition> builder)
    {
        builder.ToTable("template_roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoleCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.BuildingBlockCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => new { x.TemplateId, x.RoleCode }).IsUnique();
    }
}

internal sealed class TemplatePermissionDefinitionConfiguration : IEntityTypeConfiguration<TemplatePermissionDefinition>
{
    public void Configure(EntityTypeBuilder<TemplatePermissionDefinition> builder)
    {
        builder.ToTable("template_permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PermissionCode).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Resource).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => new { x.TemplateId, x.PermissionCode }).IsUnique();
    }
}

internal sealed class TemplateRolePermissionConfiguration : IEntityTypeConfiguration<TemplateRolePermission>
{
    public void Configure(EntityTypeBuilder<TemplateRolePermission> builder)
    {
        builder.ToTable("template_role_permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoleCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.PermissionCode).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.TemplateId, x.RoleCode, x.PermissionCode }).IsUnique();
    }
}

internal sealed class TemplateRoleHierarchyConfiguration : IEntityTypeConfiguration<TemplateRoleHierarchy>
{
    public void Configure(EntityTypeBuilder<TemplateRoleHierarchy> builder)
    {
        builder.ToTable("template_role_hierarchies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ParentRoleCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ChildRoleCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.RankOrder).IsRequired();
        builder.HasIndex(x => new { x.TemplateId, x.ParentRoleCode, x.ChildRoleCode }).IsUnique();
    }
}

internal sealed class TemplatePositionAllocationConfiguration : IEntityTypeConfiguration<TemplatePositionAllocation>
{
    public void Configure(EntityTypeBuilder<TemplatePositionAllocation> builder)
    {
        builder.ToTable("template_position_allocations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BuildingBlockCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.RoleCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.MilitaryCount).IsRequired();
        builder.Property(x => x.CivilianCount).IsRequired();
        builder.HasIndex(x => new { x.TemplateId, x.BuildingBlockCode, x.RoleCode }).IsUnique();
    }
}

internal sealed class TemplateApprovalPathConfiguration : IEntityTypeConfiguration<TemplateApprovalPath>
{
    public void Configure(EntityTypeBuilder<TemplateApprovalPath> builder)
    {
        builder.ToTable("template_approval_paths");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.WorkflowCode).HasMaxLength(120).IsRequired();
        builder.Property(x => x.FromDepartmentCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.FromBuildingBlockCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ToDepartmentCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ToBuildingBlockCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ApprovalRoleCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.StepOrder).IsRequired();
        builder.Property(x => x.CrossDepartment).IsRequired();
    }
}

internal sealed class OrganogramInstanceConfiguration : IEntityTypeConfiguration<OrganogramInstance>
{
    public void Configure(EntityTypeBuilder<OrganogramInstance> builder)
    {
        builder.ToTable("organogram_instances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.InstanceCode).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.HasIndex(x => x.InstanceCode).IsUnique();
    }
}

internal sealed class OrganizationalUnitConfiguration : IEntityTypeConfiguration<OrganizationalUnit>
{
    public void Configure(EntityTypeBuilder<OrganizationalUnit> builder)
    {
        builder.ToTable("organizational_units");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UnitKey).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UnitType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.BuildingBlockCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.DepartmentCode).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => new { x.InstanceId, x.UnitKey }).IsUnique();
    }
}

internal sealed class InstanceBuildingBlockConfiguration : IEntityTypeConfiguration<InstanceBuildingBlock>
{
    public void Configure(EntityTypeBuilder<InstanceBuildingBlock> builder)
    {
        builder.ToTable("instance_building_blocks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BuildingBlockCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DepartmentCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ParentBuildingBlockCode).HasMaxLength(80);
        builder.HasIndex(x => new { x.InstanceId, x.BuildingBlockCode }).IsUnique();
    }
}

internal sealed class InstanceRoleDefinitionConfiguration : IEntityTypeConfiguration<InstanceRoleDefinition>
{
    public void Configure(EntityTypeBuilder<InstanceRoleDefinition> builder)
    {
        builder.ToTable("instance_roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoleCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.BuildingBlockCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => new { x.InstanceId, x.RoleCode }).IsUnique();
    }
}

internal sealed class InstancePermissionDefinitionConfiguration : IEntityTypeConfiguration<InstancePermissionDefinition>
{
    public void Configure(EntityTypeBuilder<InstancePermissionDefinition> builder)
    {
        builder.ToTable("instance_permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PermissionCode).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Resource).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => new { x.InstanceId, x.PermissionCode }).IsUnique();
    }
}

internal sealed class InstanceRolePermissionConfiguration : IEntityTypeConfiguration<InstanceRolePermission>
{
    public void Configure(EntityTypeBuilder<InstanceRolePermission> builder)
    {
        builder.ToTable("instance_role_permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoleCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.PermissionCode).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.InstanceId, x.RoleCode, x.PermissionCode }).IsUnique();
    }
}

internal sealed class InstanceRoleHierarchyConfiguration : IEntityTypeConfiguration<InstanceRoleHierarchy>
{
    public void Configure(EntityTypeBuilder<InstanceRoleHierarchy> builder)
    {
        builder.ToTable("instance_role_hierarchies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ParentRoleCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ChildRoleCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.RankOrder).IsRequired();
        builder.HasIndex(x => new { x.InstanceId, x.ParentRoleCode, x.ChildRoleCode }).IsUnique();
    }
}

internal sealed class InstancePositionAllocationConfiguration : IEntityTypeConfiguration<InstancePositionAllocation>
{
    public void Configure(EntityTypeBuilder<InstancePositionAllocation> builder)
    {
        builder.ToTable("instance_position_allocations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BuildingBlockCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.RoleCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.MilitaryCount).IsRequired();
        builder.Property(x => x.CivilianCount).IsRequired();
        builder.HasIndex(x => new { x.InstanceId, x.BuildingBlockCode, x.RoleCode }).IsUnique();
    }
}

internal sealed class InstanceApprovalPathConfiguration : IEntityTypeConfiguration<InstanceApprovalPath>
{
    public void Configure(EntityTypeBuilder<InstanceApprovalPath> builder)
    {
        builder.ToTable("instance_approval_paths");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.WorkflowCode).HasMaxLength(120).IsRequired();
        builder.Property(x => x.FromDepartmentCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.FromBuildingBlockCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ToDepartmentCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ToBuildingBlockCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ApprovalRoleCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.StepOrder).IsRequired();
        builder.Property(x => x.CrossDepartment).IsRequired();
    }
}

internal sealed class UserOrganogramAssignmentConfiguration : IEntityTypeConfiguration<UserOrganogramAssignment>
{
    public void Configure(EntityTypeBuilder<UserOrganogramAssignment> builder)
    {
        builder.ToTable("user_organogram_assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BuildingBlockCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.RoleCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.InstanceId, x.RoleCode, x.OrganizationalUnitId }).IsUnique();
    }
}

internal sealed class SuperAdminAssignmentConfiguration : IEntityTypeConfiguration<SuperAdminAssignment>
{
    public void Configure(EntityTypeBuilder<SuperAdminAssignment> builder)
    {
        builder.ToTable("super_admin_assignments");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
