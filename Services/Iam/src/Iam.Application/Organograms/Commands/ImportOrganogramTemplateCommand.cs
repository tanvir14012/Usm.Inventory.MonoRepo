using Iam.Application.Abstractions;
using Iam.Application.Common;
using Iam.Application.Organograms.Dtos;
using Iam.Domain.Organograms;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.Utils.Excel.Reader.Abstractions;
using Usm.Shared.Utils.Excel.Reader.Models;

namespace Iam.Application.Organograms.Commands;

public sealed record ImportOrganogramTemplateCommand(
    string Name,
    string Version,
    byte[] FileBytes) : IRequest<OrganogramTemplateDto>;

public sealed class ImportOrganogramTemplateCommandHandler(
    IIamDbContext dbContext,
    IExcelReader excelReader) : IRequestHandler<ImportOrganogramTemplateCommand, OrganogramTemplateDto>
{
    private static class SheetIndex
    {
        public const int Departments = 0;
        public const int BuildingBlocks = 1;
        public const int Roles = 2;
        public const int Permissions = 3;
        public const int RolePermissions = 4;
        public const int RoleHierarchies = 5;
        public const int PositionAllocations = 6;
        public const int ApprovalPaths = 7;
    }

    public async Task<OrganogramTemplateDto> Handle(ImportOrganogramTemplateCommand request, CancellationToken cancellationToken)
    {
        if (request.FileBytes.Length == 0)
        {
            throw new ApplicationValidationException("The imported Excel file is empty.");
        }

        await using var fileStream = new MemoryStream(request.FileBytes, writable: false);

        var importedDepartments = await ReadSheetAsync<DepartmentImportRow>(fileStream, SheetIndex.Departments, cancellationToken);
        var importedBuildingBlocks = await ReadSheetAsync<BuildingBlockImportRow>(fileStream, SheetIndex.BuildingBlocks, cancellationToken);
        var importedRoles = await ReadSheetAsync<RoleImportRow>(fileStream, SheetIndex.Roles, cancellationToken);
        var importedPermissions = await ReadSheetAsync<PermissionImportRow>(fileStream, SheetIndex.Permissions, cancellationToken);
        var importedRolePermissions = await ReadSheetAsync<RolePermissionImportRow>(fileStream, SheetIndex.RolePermissions, cancellationToken);
        var importedRoleHierarchies = await ReadSheetAsync<RoleHierarchyImportRow>(fileStream, SheetIndex.RoleHierarchies, cancellationToken);
        var importedPositionAllocations = await ReadSheetAsync<PositionAllocationImportRow>(fileStream, SheetIndex.PositionAllocations, cancellationToken);
        var importedApprovalPaths = await ReadSheetAsync<ApprovalPathImportRow>(fileStream, SheetIndex.ApprovalPaths, cancellationToken);

        ValidateCrossReferences(
            importedDepartments,
            importedBuildingBlocks,
            importedRoles,
            importedPermissions,
            importedRolePermissions,
            importedRoleHierarchies,
            importedPositionAllocations,
            importedApprovalPaths);

        var normalizedName = request.Name.Trim();
        var normalizedVersion = request.Version.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName) || string.IsNullOrWhiteSpace(normalizedVersion))
        {
            throw new ApplicationValidationException("Template name and version are required.");
        }

        var template = await dbContext.OrganogramTemplates
            .Include(x => x.Departments)
            .Include(x => x.BuildingBlocks)
            .Include(x => x.Roles)
            .Include(x => x.Permissions)
            .Include(x => x.RolePermissions)
            .Include(x => x.RoleHierarchies)
            .Include(x => x.PositionAllocations)
            .Include(x => x.ApprovalPaths)
            .FirstOrDefaultAsync(
                x => x.Name == normalizedName && x.Version == normalizedVersion,
                cancellationToken);

        if (template is null)
        {
            template = OrganogramTemplate.Create(normalizedName, normalizedVersion);
            dbContext.OrganogramTemplates.Add(template);
        }

        template.EnsureImportAllowed();

        template.ReplaceDefinitionData(
            importedDepartments.Select(x => TemplateDepartment.Create(template.Id, x.DepartmentCode.Trim(), x.DepartmentName.Trim(), x.ParentDepartmentCode?.Trim())),
            importedBuildingBlocks.Select(x => TemplateBuildingBlock.Create(template.Id, x.BuildingBlockCode.Trim(), x.BuildingBlockName.Trim(), x.DepartmentCode.Trim(), x.ParentBuildingBlockCode?.Trim())),
            importedRoles.Select(x => TemplateRoleDefinition.Create(template.Id, x.RoleCode.Trim(), x.RoleName.Trim(), x.Description?.Trim() ?? string.Empty, x.BuildingBlockCode.Trim(), x.IsActive)),
            importedPermissions.Select(x => TemplatePermissionDefinition.Create(template.Id, x.PermissionCode.Trim(), x.PermissionName.Trim(), x.Resource.Trim(), x.Action.Trim(), x.IsActive)),
            importedRolePermissions.Select(x => TemplateRolePermission.Create(template.Id, x.RoleCode.Trim(), x.PermissionCode.Trim())),
            importedRoleHierarchies.Select(x => TemplateRoleHierarchy.Create(template.Id, x.ParentRoleCode.Trim(), x.ChildRoleCode.Trim(), x.RankOrder)),
            importedPositionAllocations.Select(x => TemplatePositionAllocation.Create(template.Id, x.BuildingBlockCode.Trim(), x.RoleCode.Trim(), x.MilitaryCount, x.CivilianCount)),
            importedApprovalPaths.Select(x =>
                TemplateApprovalPath.Create(
                    template.Id,
                    x.WorkflowCode.Trim(),
                    x.FromDepartmentCode.Trim(),
                    x.FromBuildingBlockCode.Trim(),
                    x.ToDepartmentCode.Trim(),
                    x.ToBuildingBlockCode.Trim(),
                    x.ApprovalRoleCode.Trim(),
                    x.StepOrder,
                    x.CrossDepartment)));

        await dbContext.SaveChangesAsync(cancellationToken);

        return new OrganogramTemplateDto(
            template.Id,
            template.Name,
            template.Version,
            template.Status,
            template.CreatedAt,
            template.FirstInstantiatedAt);
    }

    private async Task<IReadOnlyList<TSheetRow>> ReadSheetAsync<TSheetRow>(
        Stream fileStream,
        int sheetIndex,
        CancellationToken cancellationToken)
        where TSheetRow : class, new()
    {
        fileStream.Position = 0;
        var result = await excelReader.ReadAsync<TSheetRow>(
            fileStream,
            new ExcelReaderOptions
            {
                WorksheetIndex = sheetIndex,
                SkipHeaderRow = true,
                FailFast = false
            },
            cancellationToken);

        if (!result.HasErrors)
        {
            return result.Records;
        }

        var errorPreview = string.Join(
            "; ",
            result.Errors
                .Take(5)
                .Select(x => $"row {x.RowNumber}, property '{x.PropertyName}': {x.Message}"));

        throw new ApplicationValidationException(
            $"Excel import failed on sheet index {sheetIndex}. {errorPreview}");
    }

    private static void ValidateCrossReferences(
        IReadOnlyList<DepartmentImportRow> departments,
        IReadOnlyList<BuildingBlockImportRow> buildingBlocks,
        IReadOnlyList<RoleImportRow> roles,
        IReadOnlyList<PermissionImportRow> permissions,
        IReadOnlyList<RolePermissionImportRow> rolePermissions,
        IReadOnlyList<RoleHierarchyImportRow> roleHierarchies,
        IReadOnlyList<PositionAllocationImportRow> positionAllocations,
        IReadOnlyList<ApprovalPathImportRow> approvalPaths)
    {
        EnsureUnique(departments.Select(x => x.DepartmentCode), "department code");
        EnsureUnique(buildingBlocks.Select(x => x.BuildingBlockCode), "building block code");
        EnsureUnique(roles.Select(x => x.RoleCode), "role code");
        EnsureUnique(permissions.Select(x => x.PermissionCode), "permission code");

        var departmentCodes = departments.Select(x => x.DepartmentCode.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var buildingBlockCodes = buildingBlocks.Select(x => x.BuildingBlockCode.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var roleCodes = roles.Select(x => x.RoleCode.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var permissionCodes = permissions.Select(x => x.PermissionCode.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        ValidateReferences(buildingBlocks, x => x.DepartmentCode, departmentCodes, "building block department code");
        ValidateReferences(roles, x => x.BuildingBlockCode, buildingBlockCodes, "role building block code");
        ValidateReferences(rolePermissions, x => x.RoleCode, roleCodes, "role-permission role code");
        ValidateReferences(rolePermissions, x => x.PermissionCode, permissionCodes, "role-permission permission code");
        ValidateReferences(roleHierarchies, x => x.ParentRoleCode, roleCodes, "role hierarchy parent role code");
        ValidateReferences(roleHierarchies, x => x.ChildRoleCode, roleCodes, "role hierarchy child role code");
        ValidateReferences(positionAllocations, x => x.BuildingBlockCode, buildingBlockCodes, "position allocation building block code");
        ValidateReferences(positionAllocations, x => x.RoleCode, roleCodes, "position allocation role code");
        ValidateReferences(approvalPaths, x => x.FromDepartmentCode, departmentCodes, "approval path from department code");
        ValidateReferences(approvalPaths, x => x.ToDepartmentCode, departmentCodes, "approval path to department code");
        ValidateReferences(approvalPaths, x => x.FromBuildingBlockCode, buildingBlockCodes, "approval path from building block code");
        ValidateReferences(approvalPaths, x => x.ToBuildingBlockCode, buildingBlockCodes, "approval path to building block code");
        ValidateReferences(approvalPaths, x => x.ApprovalRoleCode, roleCodes, "approval path role code");

        if (roles.Any(x => x.RoleCode.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) || x.RoleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ApplicationValidationException("SuperAdmin role is system-reserved and cannot be defined in organogram templates.");
        }
    }

    private static void EnsureUnique(IEnumerable<string?> values, string label)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ApplicationValidationException($"A required {label} is missing.");
            }

            if (!seen.Add(normalized))
            {
                throw new ApplicationValidationException($"Duplicate {label} '{normalized}' was found.");
            }
        }
    }

    private static void ValidateReferences<T>(
        IEnumerable<T> rows,
        Func<T, string?> selector,
        HashSet<string> validCodes,
        string label)
    {
        foreach (var row in rows)
        {
            var value = selector(row)?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ApplicationValidationException($"A required {label} is missing.");
            }

            if (!validCodes.Contains(value))
            {
                throw new ApplicationValidationException($"Unknown {label} '{value}' was found.");
            }
        }
    }

    public sealed class DepartmentImportRow
    {
        public string DepartmentCode { get; init; } = string.Empty;
        public string DepartmentName { get; init; } = string.Empty;
        public string? ParentDepartmentCode { get; init; }
    }

    public sealed class BuildingBlockImportRow
    {
        public string BuildingBlockCode { get; init; } = string.Empty;
        public string BuildingBlockName { get; init; } = string.Empty;
        public string DepartmentCode { get; init; } = string.Empty;
        public string? ParentBuildingBlockCode { get; init; }
    }

    public sealed class RoleImportRow
    {
        public string RoleCode { get; init; } = string.Empty;
        public string RoleName { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string BuildingBlockCode { get; init; } = string.Empty;
        public bool IsActive { get; init; } = true;
    }

    public sealed class PermissionImportRow
    {
        public string PermissionCode { get; init; } = string.Empty;
        public string PermissionName { get; init; } = string.Empty;
        public string Resource { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public bool IsActive { get; init; } = true;
    }

    public sealed class RolePermissionImportRow
    {
        public string RoleCode { get; init; } = string.Empty;
        public string PermissionCode { get; init; } = string.Empty;
    }

    public sealed class RoleHierarchyImportRow
    {
        public string ParentRoleCode { get; init; } = string.Empty;
        public string ChildRoleCode { get; init; } = string.Empty;
        public int RankOrder { get; init; }
    }

    public sealed class PositionAllocationImportRow
    {
        public string BuildingBlockCode { get; init; } = string.Empty;
        public string RoleCode { get; init; } = string.Empty;
        public int MilitaryCount { get; init; }
        public int CivilianCount { get; init; }
    }

    public sealed class ApprovalPathImportRow
    {
        public string WorkflowCode { get; init; } = string.Empty;
        public string FromDepartmentCode { get; init; } = string.Empty;
        public string FromBuildingBlockCode { get; init; } = string.Empty;
        public string ToDepartmentCode { get; init; } = string.Empty;
        public string ToBuildingBlockCode { get; init; } = string.Empty;
        public string ApprovalRoleCode { get; init; } = string.Empty;
        public int StepOrder { get; init; }
        public bool CrossDepartment { get; init; }
    }
}
