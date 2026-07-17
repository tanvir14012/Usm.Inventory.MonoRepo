using Iam.Domain.Common;
using Usm.Shared.Data.DbContextExtensions;

namespace Iam.Domain.Organograms;

public enum OrganogramInstanceStatus
{
    Active = 1,
    Inactive = 2
}

public sealed class OrganogramInstance : AggregateRoot<Guid>, IAuditable
{
    private readonly List<OrganizationalUnit> _organizationalUnits = [];
    private readonly List<InstanceBuildingBlock> _buildingBlocks = [];
    private readonly List<InstanceRoleDefinition> _roles = [];
    private readonly List<InstancePermissionDefinition> _permissions = [];
    private readonly List<InstanceRolePermission> _rolePermissions = [];
    private readonly List<InstanceRoleHierarchy> _roleHierarchies = [];
    private readonly List<InstancePositionAllocation> _positionAllocations = [];
    private readonly List<InstanceApprovalPath> _approvalPaths = [];

    public Guid TemplateId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string InstanceCode { get; private set; } = string.Empty;
    public OrganogramInstanceStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public IReadOnlyCollection<OrganizationalUnit> OrganizationalUnits => _organizationalUnits.AsReadOnly();
    public IReadOnlyCollection<InstanceBuildingBlock> BuildingBlocks => _buildingBlocks.AsReadOnly();
    public IReadOnlyCollection<InstanceRoleDefinition> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<InstancePermissionDefinition> Permissions => _permissions.AsReadOnly();
    public IReadOnlyCollection<InstanceRolePermission> RolePermissions => _rolePermissions.AsReadOnly();
    public IReadOnlyCollection<InstanceRoleHierarchy> RoleHierarchies => _roleHierarchies.AsReadOnly();
    public IReadOnlyCollection<InstancePositionAllocation> PositionAllocations => _positionAllocations.AsReadOnly();
    public IReadOnlyCollection<InstanceApprovalPath> ApprovalPaths => _approvalPaths.AsReadOnly();

    private OrganogramInstance() { }

    public static OrganogramInstance Create(Guid templateId, string name, string instanceCode)
    {
        return new OrganogramInstance
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            Name = name,
            InstanceCode = instanceCode,
            Status = OrganogramInstanceStatus.Active
        };
    }

    public void AddOrganizationalUnit(OrganizationalUnit unit) => _organizationalUnits.Add(unit);
    public void AddBuildingBlocks(IEnumerable<InstanceBuildingBlock> rows) => _buildingBlocks.AddRange(rows);
    public void AddRoles(IEnumerable<InstanceRoleDefinition> rows) => _roles.AddRange(rows);
    public void AddPermissions(IEnumerable<InstancePermissionDefinition> rows) => _permissions.AddRange(rows);
    public void AddRolePermissions(IEnumerable<InstanceRolePermission> rows) => _rolePermissions.AddRange(rows);
    public void AddRoleHierarchies(IEnumerable<InstanceRoleHierarchy> rows) => _roleHierarchies.AddRange(rows);
    public void AddPositionAllocations(IEnumerable<InstancePositionAllocation> rows) => _positionAllocations.AddRange(rows);
    public void AddApprovalPaths(IEnumerable<InstanceApprovalPath> rows) => _approvalPaths.AddRange(rows);
}

public sealed class OrganizationalUnit : EntityBase<Guid>
{
    public Guid InstanceId { get; private set; }
    public Guid? ParentUnitId { get; private set; }
    public string UnitKey { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string UnitType { get; private set; } = string.Empty;
    public string BuildingBlockCode { get; private set; } = string.Empty;
    public string DepartmentCode { get; private set; } = string.Empty;

    private OrganizationalUnit() { }

    public static OrganizationalUnit Create(
        Guid instanceId,
        Guid? parentUnitId,
        string unitKey,
        string name,
        string unitType,
        string buildingBlockCode,
        string departmentCode)
    {
        return new OrganizationalUnit
        {
            Id = Guid.NewGuid(),
            InstanceId = instanceId,
            ParentUnitId = parentUnitId,
            UnitKey = unitKey,
            Name = name,
            UnitType = unitType,
            BuildingBlockCode = buildingBlockCode,
            DepartmentCode = departmentCode
        };
    }
}

public sealed class InstanceBuildingBlock : EntityBase<Guid>
{
    public Guid InstanceId { get; private set; }
    public string BuildingBlockCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string DepartmentCode { get; private set; } = string.Empty;
    public string? ParentBuildingBlockCode { get; private set; }

    private InstanceBuildingBlock() { }

    public static InstanceBuildingBlock Create(
        Guid instanceId,
        string buildingBlockCode,
        string name,
        string departmentCode,
        string? parentBuildingBlockCode)
    {
        return new InstanceBuildingBlock
        {
            Id = Guid.NewGuid(),
            InstanceId = instanceId,
            BuildingBlockCode = buildingBlockCode,
            Name = name,
            DepartmentCode = departmentCode,
            ParentBuildingBlockCode = parentBuildingBlockCode
        };
    }
}

public sealed class InstanceRoleDefinition : EntityBase<Guid>
{
    public Guid InstanceId { get; private set; }
    public string RoleCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string BuildingBlockCode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private InstanceRoleDefinition() { }

    public static InstanceRoleDefinition Create(
        Guid instanceId,
        string roleCode,
        string name,
        string description,
        string buildingBlockCode,
        bool isActive)
    {
        return new InstanceRoleDefinition
        {
            Id = Guid.NewGuid(),
            InstanceId = instanceId,
            RoleCode = roleCode,
            Name = name,
            Description = description,
            BuildingBlockCode = buildingBlockCode,
            IsActive = isActive
        };
    }
}

public sealed class InstancePermissionDefinition : EntityBase<Guid>
{
    public Guid InstanceId { get; private set; }
    public string PermissionCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private InstancePermissionDefinition() { }

    public static InstancePermissionDefinition Create(
        Guid instanceId,
        string permissionCode,
        string name,
        string resource,
        string action,
        bool isActive)
    {
        return new InstancePermissionDefinition
        {
            Id = Guid.NewGuid(),
            InstanceId = instanceId,
            PermissionCode = permissionCode,
            Name = name,
            Resource = resource,
            Action = action,
            IsActive = isActive
        };
    }
}

public sealed class InstanceRolePermission : EntityBase<Guid>
{
    public Guid InstanceId { get; private set; }
    public string RoleCode { get; private set; } = string.Empty;
    public string PermissionCode { get; private set; } = string.Empty;

    private InstanceRolePermission() { }

    public static InstanceRolePermission Create(Guid instanceId, string roleCode, string permissionCode)
    {
        return new InstanceRolePermission
        {
            Id = Guid.NewGuid(),
            InstanceId = instanceId,
            RoleCode = roleCode,
            PermissionCode = permissionCode
        };
    }
}

public sealed class InstanceRoleHierarchy : EntityBase<Guid>
{
    public Guid InstanceId { get; private set; }
    public string ParentRoleCode { get; private set; } = string.Empty;
    public string ChildRoleCode { get; private set; } = string.Empty;
    public int RankOrder { get; private set; }

    private InstanceRoleHierarchy() { }

    public static InstanceRoleHierarchy Create(Guid instanceId, string parentRoleCode, string childRoleCode, int rankOrder)
    {
        return new InstanceRoleHierarchy
        {
            Id = Guid.NewGuid(),
            InstanceId = instanceId,
            ParentRoleCode = parentRoleCode,
            ChildRoleCode = childRoleCode,
            RankOrder = rankOrder
        };
    }
}

public sealed class InstancePositionAllocation : EntityBase<Guid>
{
    public Guid InstanceId { get; private set; }
    public string BuildingBlockCode { get; private set; } = string.Empty;
    public string RoleCode { get; private set; } = string.Empty;
    public int MilitaryCount { get; private set; }
    public int CivilianCount { get; private set; }

    private InstancePositionAllocation() { }

    public static InstancePositionAllocation Create(
        Guid instanceId,
        string buildingBlockCode,
        string roleCode,
        int militaryCount,
        int civilianCount)
    {
        return new InstancePositionAllocation
        {
            Id = Guid.NewGuid(),
            InstanceId = instanceId,
            BuildingBlockCode = buildingBlockCode,
            RoleCode = roleCode,
            MilitaryCount = militaryCount,
            CivilianCount = civilianCount
        };
    }
}

public sealed class InstanceApprovalPath : EntityBase<Guid>
{
    public Guid InstanceId { get; private set; }
    public string WorkflowCode { get; private set; } = string.Empty;
    public string FromDepartmentCode { get; private set; } = string.Empty;
    public string FromBuildingBlockCode { get; private set; } = string.Empty;
    public string ToDepartmentCode { get; private set; } = string.Empty;
    public string ToBuildingBlockCode { get; private set; } = string.Empty;
    public string ApprovalRoleCode { get; private set; } = string.Empty;
    public int StepOrder { get; private set; }
    public bool CrossDepartment { get; private set; }

    private InstanceApprovalPath() { }

    public static InstanceApprovalPath Create(
        Guid instanceId,
        string workflowCode,
        string fromDepartmentCode,
        string fromBuildingBlockCode,
        string toDepartmentCode,
        string toBuildingBlockCode,
        string approvalRoleCode,
        int stepOrder,
        bool crossDepartment)
    {
        return new InstanceApprovalPath
        {
            Id = Guid.NewGuid(),
            InstanceId = instanceId,
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

public sealed class UserOrganogramAssignment : EntityBase<Guid>, IAuditable
{
    public Guid UserId { get; private set; }
    public Guid InstanceId { get; private set; }
    public Guid OrganizationalUnitId { get; private set; }
    public string BuildingBlockCode { get; private set; } = string.Empty;
    public string RoleCode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    private UserOrganogramAssignment() { }

    public static UserOrganogramAssignment Create(
        Guid userId,
        Guid instanceId,
        Guid organizationalUnitId,
        string buildingBlockCode,
        string roleCode)
    {
        return new UserOrganogramAssignment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InstanceId = instanceId,
            OrganizationalUnitId = organizationalUnitId,
            BuildingBlockCode = buildingBlockCode,
            RoleCode = roleCode,
            IsActive = true
        };
    }
}

public sealed class SuperAdminAssignment : EntityBase<Guid>, IAuditable
{
    public Guid UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    private SuperAdminAssignment() { }

    public static SuperAdminAssignment Create(Guid userId)
    {
        return new SuperAdminAssignment
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };
    }
}
