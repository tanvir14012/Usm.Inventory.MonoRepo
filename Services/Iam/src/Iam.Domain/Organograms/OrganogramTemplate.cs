using Iam.Domain.Common;
using Usm.Shared.Data.DbContextExtensions;

namespace Iam.Domain.Organograms;

public enum OrganogramTemplateStatus
{
    Draft = 1,
    Locked = 2
}

public sealed class OrganogramTemplate : AggregateRoot<Guid>, IAuditable
{
    private readonly List<TemplateDepartment> _departments = [];
    private readonly List<TemplateBuildingBlock> _buildingBlocks = [];
    private readonly List<TemplateRoleDefinition> _roles = [];
    private readonly List<TemplatePermissionDefinition> _permissions = [];
    private readonly List<TemplateRolePermission> _rolePermissions = [];
    private readonly List<TemplateRoleHierarchy> _roleHierarchies = [];
    private readonly List<TemplatePositionAllocation> _positionAllocations = [];
    private readonly List<TemplateApprovalPath> _approvalPaths = [];

    public string Name { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public OrganogramTemplateStatus Status { get; private set; } = OrganogramTemplateStatus.Draft;
    public DateTimeOffset? FirstInstantiatedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public IReadOnlyCollection<TemplateDepartment> Departments => _departments.AsReadOnly();
    public IReadOnlyCollection<TemplateBuildingBlock> BuildingBlocks => _buildingBlocks.AsReadOnly();
    public IReadOnlyCollection<TemplateRoleDefinition> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<TemplatePermissionDefinition> Permissions => _permissions.AsReadOnly();
    public IReadOnlyCollection<TemplateRolePermission> RolePermissions => _rolePermissions.AsReadOnly();
    public IReadOnlyCollection<TemplateRoleHierarchy> RoleHierarchies => _roleHierarchies.AsReadOnly();
    public IReadOnlyCollection<TemplatePositionAllocation> PositionAllocations => _positionAllocations.AsReadOnly();
    public IReadOnlyCollection<TemplateApprovalPath> ApprovalPaths => _approvalPaths.AsReadOnly();

    private OrganogramTemplate() { }

    public static OrganogramTemplate Create(string name, string version)
    {
        return new OrganogramTemplate
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Version = version.Trim(),
            Status = OrganogramTemplateStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void EnsureImportAllowed()
    {
        if (Status == OrganogramTemplateStatus.Locked)
        {
            throw new InvalidOperationException(
                $"Organogram template '{Name}' version '{Version}' is immutable after instantiation.");
        }
    }

    public void ReplaceDefinitionData(
        IEnumerable<TemplateDepartment> departments,
        IEnumerable<TemplateBuildingBlock> buildingBlocks,
        IEnumerable<TemplateRoleDefinition> roles,
        IEnumerable<TemplatePermissionDefinition> permissions,
        IEnumerable<TemplateRolePermission> rolePermissions,
        IEnumerable<TemplateRoleHierarchy> roleHierarchies,
        IEnumerable<TemplatePositionAllocation> positionAllocations,
        IEnumerable<TemplateApprovalPath> approvalPaths)
    {
        EnsureImportAllowed();

        _departments.Clear();
        _buildingBlocks.Clear();
        _roles.Clear();
        _permissions.Clear();
        _rolePermissions.Clear();
        _roleHierarchies.Clear();
        _positionAllocations.Clear();
        _approvalPaths.Clear();

        _departments.AddRange(departments);
        _buildingBlocks.AddRange(buildingBlocks);
        _roles.AddRange(roles);
        _permissions.AddRange(permissions);
        _rolePermissions.AddRange(rolePermissions);
        _roleHierarchies.AddRange(roleHierarchies);
        _positionAllocations.AddRange(positionAllocations);
        _approvalPaths.AddRange(approvalPaths);
    }

    public void MarkInstantiated()
    {
        if (Status == OrganogramTemplateStatus.Locked)
        {
            return;
        }

        Status = OrganogramTemplateStatus.Locked;
        FirstInstantiatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class TemplateDepartment : EntityBase<Guid>
{
    public Guid TemplateId { get; private set; }
    public string DepartmentCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ParentDepartmentCode { get; private set; }

    private TemplateDepartment() { }

    public static TemplateDepartment Create(
        Guid templateId,
        string departmentCode,
        string name,
        string? parentDepartmentCode)
    {
        return new TemplateDepartment
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            DepartmentCode = departmentCode,
            Name = name,
            ParentDepartmentCode = parentDepartmentCode
        };
    }
}

public sealed class TemplateBuildingBlock : EntityBase<Guid>
{
    public Guid TemplateId { get; private set; }
    public string BuildingBlockCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string DepartmentCode { get; private set; } = string.Empty;
    public string? ParentBuildingBlockCode { get; private set; }

    private TemplateBuildingBlock() { }

    public static TemplateBuildingBlock Create(
        Guid templateId,
        string buildingBlockCode,
        string name,
        string departmentCode,
        string? parentBuildingBlockCode)
    {
        return new TemplateBuildingBlock
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            BuildingBlockCode = buildingBlockCode,
            Name = name,
            DepartmentCode = departmentCode,
            ParentBuildingBlockCode = parentBuildingBlockCode
        };
    }
}

public sealed class TemplateRoleDefinition : EntityBase<Guid>
{
    public Guid TemplateId { get; private set; }
    public string RoleCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string BuildingBlockCode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private TemplateRoleDefinition() { }

    public static TemplateRoleDefinition Create(
        Guid templateId,
        string roleCode,
        string name,
        string description,
        string buildingBlockCode,
        bool isActive)
    {
        return new TemplateRoleDefinition
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            RoleCode = roleCode,
            Name = name,
            Description = description,
            BuildingBlockCode = buildingBlockCode,
            IsActive = isActive
        };
    }
}

public sealed class TemplatePermissionDefinition : EntityBase<Guid>
{
    public Guid TemplateId { get; private set; }
    public string PermissionCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private TemplatePermissionDefinition() { }

    public static TemplatePermissionDefinition Create(
        Guid templateId,
        string permissionCode,
        string name,
        string resource,
        string action,
        bool isActive)
    {
        return new TemplatePermissionDefinition
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            PermissionCode = permissionCode,
            Name = name,
            Resource = resource,
            Action = action,
            IsActive = isActive
        };
    }
}

public sealed class TemplateRolePermission : EntityBase<Guid>
{
    public Guid TemplateId { get; private set; }
    public string RoleCode { get; private set; } = string.Empty;
    public string PermissionCode { get; private set; } = string.Empty;

    private TemplateRolePermission() { }

    public static TemplateRolePermission Create(Guid templateId, string roleCode, string permissionCode)
    {
        return new TemplateRolePermission
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            RoleCode = roleCode,
            PermissionCode = permissionCode
        };
    }
}

public sealed class TemplateRoleHierarchy : EntityBase<Guid>
{
    public Guid TemplateId { get; private set; }
    public string ParentRoleCode { get; private set; } = string.Empty;
    public string ChildRoleCode { get; private set; } = string.Empty;
    public int RankOrder { get; private set; }

    private TemplateRoleHierarchy() { }

    public static TemplateRoleHierarchy Create(Guid templateId, string parentRoleCode, string childRoleCode, int rankOrder)
    {
        return new TemplateRoleHierarchy
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            ParentRoleCode = parentRoleCode,
            ChildRoleCode = childRoleCode,
            RankOrder = rankOrder
        };
    }
}

public sealed class TemplatePositionAllocation : EntityBase<Guid>
{
    public Guid TemplateId { get; private set; }
    public string BuildingBlockCode { get; private set; } = string.Empty;
    public string RoleCode { get; private set; } = string.Empty;
    public int MilitaryCount { get; private set; }
    public int CivilianCount { get; private set; }

    private TemplatePositionAllocation() { }

    public static TemplatePositionAllocation Create(
        Guid templateId,
        string buildingBlockCode,
        string roleCode,
        int militaryCount,
        int civilianCount)
    {
        return new TemplatePositionAllocation
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            BuildingBlockCode = buildingBlockCode,
            RoleCode = roleCode,
            MilitaryCount = militaryCount,
            CivilianCount = civilianCount
        };
    }
}

public sealed class TemplateApprovalPath : EntityBase<Guid>
{
    public Guid TemplateId { get; private set; }
    public string WorkflowCode { get; private set; } = string.Empty;
    public string FromDepartmentCode { get; private set; } = string.Empty;
    public string FromBuildingBlockCode { get; private set; } = string.Empty;
    public string ToDepartmentCode { get; private set; } = string.Empty;
    public string ToBuildingBlockCode { get; private set; } = string.Empty;
    public string ApprovalRoleCode { get; private set; } = string.Empty;
    public int StepOrder { get; private set; }
    public bool CrossDepartment { get; private set; }

    private TemplateApprovalPath() { }

    public static TemplateApprovalPath Create(
        Guid templateId,
        string workflowCode,
        string fromDepartmentCode,
        string fromBuildingBlockCode,
        string toDepartmentCode,
        string toBuildingBlockCode,
        string approvalRoleCode,
        int stepOrder,
        bool crossDepartment)
    {
        return new TemplateApprovalPath
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            WorkflowCode = workflowCode,
            FromDepartmentCode = fromDepartmentCode,
            FromBuildingBlockCode = fromBuildingBlockCode,
            ToDepartmentCode = toDepartmentCode,
            ToBuildingBlockCode = toBuildingBlockCode,
            ApprovalRoleCode = approvalRoleCode,
            StepOrder = stepOrder,
            CrossDepartment = crossDepartment
        };
    }
}
