using Iam.Application.Abstractions;
using Iam.Application.Common;
using Iam.Application.Organograms.Dtos;
using Iam.Domain.Organograms;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Iam.Application.Organograms.Commands;

public sealed record InstantiateOrganogramTemplateCommand(
    Guid TemplateId,
    string InstanceName,
    string InstanceCode,
    IReadOnlyList<InstantiateOrganizationalUnitInput> OrganizationalUnits) : IRequest<OrganogramInstanceDto>;

public sealed record InstantiateOrganizationalUnitInput(
    string UnitKey,
    string Name,
    string UnitType,
    string BuildingBlockCode,
    string DepartmentCode,
    string? ParentUnitKey);

public sealed class InstantiateOrganogramTemplateCommandHandler(IIamDbContext dbContext)
    : IRequestHandler<InstantiateOrganogramTemplateCommand, OrganogramInstanceDto>
{
    public async Task<OrganogramInstanceDto> Handle(InstantiateOrganogramTemplateCommand request, CancellationToken cancellationToken)
    {
        if (request.OrganizationalUnits.Count == 0)
        {
            throw new ApplicationValidationException("At least one organizational unit is required for instantiation.");
        }

        var template = await dbContext.OrganogramTemplates
            .Include(x => x.BuildingBlocks)
            .Include(x => x.Roles)
            .Include(x => x.Permissions)
            .Include(x => x.RolePermissions)
            .Include(x => x.RoleHierarchies)
            .Include(x => x.PositionAllocations)
            .Include(x => x.ApprovalPaths)
            .FirstOrDefaultAsync(x => x.Id == request.TemplateId, cancellationToken);

        if (template is null)
        {
            throw new ApplicationValidationException("Template was not found.");
        }

        if (!template.Roles.Any() || !template.Permissions.Any())
        {
            throw new ApplicationValidationException("Template cannot be instantiated without roles and permissions.");
        }

        var instance = OrganogramInstance.Create(
            template.Id,
            request.InstanceName.Trim(),
            request.InstanceCode.Trim());

        var unitByKey = new Dictionary<string, OrganizationalUnit>(StringComparer.OrdinalIgnoreCase);
        foreach (var unit in request.OrganizationalUnits)
        {
            if (unitByKey.ContainsKey(unit.UnitKey))
            {
                throw new ApplicationValidationException($"Duplicate organizational unit key '{unit.UnitKey}'.");
            }

            Guid? parentUnitId = null;
            if (!string.IsNullOrWhiteSpace(unit.ParentUnitKey))
            {
                if (!unitByKey.TryGetValue(unit.ParentUnitKey, out var parent))
                {
                    throw new ApplicationValidationException(
                        $"Parent unit key '{unit.ParentUnitKey}' for unit '{unit.UnitKey}' was not found. Provide parent units before children.");
                }

                parentUnitId = parent.Id;
            }

            var created = OrganizationalUnit.Create(
                instance.Id,
                parentUnitId,
                unit.UnitKey.Trim(),
                unit.Name.Trim(),
                unit.UnitType.Trim(),
                unit.BuildingBlockCode.Trim(),
                unit.DepartmentCode.Trim());

            instance.AddOrganizationalUnit(created);
            unitByKey.Add(unit.UnitKey, created);
        }

        instance.AddBuildingBlocks(template.BuildingBlocks.Select(x =>
            InstanceBuildingBlock.Create(instance.Id, x.BuildingBlockCode, x.Name, x.DepartmentCode, x.ParentBuildingBlockCode)));

        instance.AddRoles(template.Roles.Select(x =>
            InstanceRoleDefinition.Create(instance.Id, x.RoleCode, x.Name, x.Description, x.BuildingBlockCode, x.IsActive)));

        instance.AddPermissions(template.Permissions.Select(x =>
            InstancePermissionDefinition.Create(instance.Id, x.PermissionCode, x.Name, x.Resource, x.Action, x.IsActive)));

        instance.AddRolePermissions(template.RolePermissions.Select(x =>
            InstanceRolePermission.Create(instance.Id, x.RoleCode, x.PermissionCode)));

        instance.AddRoleHierarchies(template.RoleHierarchies.Select(x =>
            InstanceRoleHierarchy.Create(instance.Id, x.ParentRoleCode, x.ChildRoleCode, x.RankOrder)));

        instance.AddPositionAllocations(template.PositionAllocations.Select(x =>
            InstancePositionAllocation.Create(instance.Id, x.BuildingBlockCode, x.RoleCode, x.MilitaryCount, x.CivilianCount)));

        instance.AddApprovalPaths(template.ApprovalPaths.Select(x =>
            InstanceApprovalPath.Create(
                instance.Id,
                x.WorkflowCode,
                x.FromDepartmentCode,
                x.FromBuildingBlockCode,
                x.ToDepartmentCode,
                x.ToBuildingBlockCode,
                x.ApprovalRoleCode,
                x.StepOrder,
                x.CrossDepartment)));

        template.MarkInstantiated();

        dbContext.OrganogramInstances.Add(instance);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new OrganogramInstanceDto(
            instance.Id,
            instance.TemplateId,
            instance.Name,
            instance.InstanceCode,
            instance.OrganizationalUnits.Count,
            instance.CreatedAt);
    }
}
