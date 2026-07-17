using Iam.Application.Common;
using Iam.Application.Navigation.Commands;
using Iam.Application.Navigation.Dtos;
using MediatR;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Iam.Api.Endpoints.Navigation;

public sealed class UpdateModuleNavigationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/navigation/modules/{moduleId:guid}", Handle)
            .WithName("UpdateModuleNavigation")
            .WithTags("Navigation")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        Guid moduleId,
        UpdateModuleNavigationRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new UpdateModuleNavigationCommand(moduleId, request.Module),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (ApplicationValidationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }
}

public sealed record UpdateModuleNavigationRequest(
    ModuleNavigationInput Module);
