using Iam.Application.Abstractions;
using Iam.Application.Common;
using Iam.Domain.Organograms;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Iam.Application.Organograms.Commands;

public sealed record AssignUserToBuildingBlockCommand(
    Guid UserId,
    Guid InstanceId,
    Guid OrganizationalUnitId,
    string BuildingBlockCode,
    string RoleCode,
    bool AsSuperAdmin) : IRequest<Guid>;

public sealed class AssignUserToBuildingBlockCommandHandler(IIamDbContext dbContext)
    : IRequestHandler<AssignUserToBuildingBlockCommand, Guid>
{
    public async Task<Guid> Handle(AssignUserToBuildingBlockCommand request, CancellationToken cancellationToken)
    {
        if (request.AsSuperAdmin)
        {
            var existing = await dbContext.SuperAdminAssignments
                .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

            if (existing is null)
            {
                var superAdmin = SuperAdminAssignment.Create(request.UserId);
                dbContext.SuperAdminAssignments.Add(superAdmin);
                await dbContext.SaveChangesAsync(cancellationToken);
                return superAdmin.Id;
            }

            return existing.Id;
        }

        var roleExists = await dbContext.InstanceRoles.AnyAsync(
            x => x.InstanceId == request.InstanceId && x.RoleCode == request.RoleCode,
            cancellationToken);

        if (!roleExists)
        {
            throw new ApplicationValidationException("The selected role does not exist in this instance.");
        }

        var unitExists = await dbContext.OrganizationalUnits.AnyAsync(
            x => x.Id == request.OrganizationalUnitId &&
                 x.InstanceId == request.InstanceId &&
                 x.BuildingBlockCode == request.BuildingBlockCode,
            cancellationToken);

        if (!unitExists)
        {
            throw new ApplicationValidationException("The selected organizational unit/building block was not found in this instance.");
        }

        var assignment = UserOrganogramAssignment.Create(
            request.UserId,
            request.InstanceId,
            request.OrganizationalUnitId,
            request.BuildingBlockCode.Trim(),
            request.RoleCode.Trim());

        dbContext.UserOrganogramAssignments.Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return assignment.Id;
    }
}
