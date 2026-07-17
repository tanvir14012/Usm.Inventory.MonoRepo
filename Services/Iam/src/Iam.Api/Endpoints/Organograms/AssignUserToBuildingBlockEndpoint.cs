using Iam.Application.Authorization;
using Iam.Application.Common;
using Iam.Application.Organograms.Commands;
using MediatR;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Iam.Api.Endpoints.Organograms;

public sealed class AssignUserToBuildingBlockEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/organogram/users/assignments", Handle)
            .WithName("AssignUserToBuildingBlock")
            .WithTags("Organograms")
            .RequireAuthorization(PermissionPolicies.Permission(IamPermissions.OrganogramUserAssign));
    }

    private static async Task<IResult> Handle(
        AssignUserToBuildingBlockRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var assignmentId = await sender.Send(
                new AssignUserToBuildingBlockCommand(
                    request.UserId,
                    request.InstanceId,
                    request.OrganizationalUnitId,
                    request.BuildingBlockCode,
                    request.RoleCode,
                    request.AsSuperAdmin),
                cancellationToken);

            return Results.Ok(new { AssignmentId = assignmentId });
        }
        catch (ApplicationValidationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }
}

public sealed record AssignUserToBuildingBlockRequest(
    Guid UserId,
    Guid InstanceId,
    Guid OrganizationalUnitId,
    string BuildingBlockCode,
    string RoleCode,
    bool AsSuperAdmin);
